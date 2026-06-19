package com.jatin.runapp.webrtc

import android.content.Context
import android.view.Surface
import androidx.lifecycle.DefaultLifecycleObserver
import androidx.lifecycle.LifecycleOwner
import com.jatin.runapp.data.LyfronBridge
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import java.util.concurrent.ConcurrentHashMap

class WebRTCManager(
    private val context: Context,
    private val authToken: String,
    private val serverUrl: String = "https://api.runapp.in"
) : DefaultLifecycleObserver {

    private val lyfron = LyfronBridge.instance
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    
    private var signaling: HubConnection? = null
    private var currentCall: ActiveCall? = null
    
    private val _callState = MutableStateFlow<CallState>(CallState.Idle)
    val callState: StateFlow<CallState> = _callState
    
    private val _remoteVideoSurface = MutableStateFlow<Surface?>(null)
    val remoteVideoSurface: StateFlow<Surface?> = _remoteVideoSurface

    private val pendingIceCandidates = ConcurrentHashMap<Long, MutableList<IceCandidate>>()
    
    data class ActiveCall(
        val handle: Long,
        val callId: String,
        val remoteUserId: String,
        val isVideo: Boolean,
        val isCaller: Boolean
    )

    init {
        NativeWebRTC.initialize()
        connectSignaling()
    }

    private fun connectSignaling() {
        signaling = HubConnectionBuilder.create("$serverUrl/hubs/call")
            .withAccessToken { Single.just(authToken) }
            .build()

        signaling?.on("IncomingCall", { callId: String, callerId: String, callerName: String, callType: String ->
            scope.launch(Dispatchers.Main) {
                _callState.value = CallState.Incoming(
                    callId = callId,
                    callerId = callerId,
                    callerName = callerName,
                    isVideo = callType == "video"
                )
            }
        }, String::class.java, String::class.java, String::class.java, String::class.java)

        signaling?.on("CallAccepted", { callId: String, answerSdp: String ->
            scope.launch {
                currentCall?.let { call ->
                    NativeWebRTC.setRemoteDescription(call.handle, "answer", answerSdp)
                    drainIceCandidates(call.handle)
                }
            }
        }, String::class.java, String::class.java)

        signaling?.on("IceCandidate", { callId: String, sdpMid: String, index: Int, candidate: String ->
            scope.launch {
                currentCall?.let { call ->
                    val ice = IceCandidate(sdpMid, index, candidate)
                    if (call.handle != 0L) {
                        NativeWebRTC.addIceCandidate(call.handle, sdpMid, index, candidate)
                    } else {
                        pendingIceCandidates.getOrPut(call.handle) { mutableListOf() }.add(ice)
                    }
                }
            }
        }, String::class.java, String::class.java, Int::class.java, String::class.java)

        signaling?.on("CallEnded", { callId: String, reason: String ->
            endCall()
        }, String::class.java, String::class.java)

        signaling?.start()?.blockingAwait()
    }

    fun startCall(remoteUserId: String, isVideo: Boolean, localSurface: Surface?) {
        if (_callState.value !is CallState.Idle) {
            throw IllegalStateException("Already in a call")
        }

        // Lyfron threat check before call
        scope.launch {
            val threat = lyfron.checkThreat(
                userID = getCurrentUserId(),
                action = "call:outgoing",
                ip = getDeviceIP()
            )
            if (!threat.allowed) {
                _callState.value = CallState.Error("Call blocked by security")
                return@launch
            }

            _callState.value = CallState.Connecting

            val handle = NativeWebRTC.createPeerConnection("stun:stun.l.google.com:19302")
            
            NativeWebRTC.addAudioTrack(handle)
            if (isVideo && localSurface != null) {
                NativeWebRTC.addVideoTrack(handle, localSurface)
            }

            val callId = generateCallId()
            currentCall = ActiveCall(handle, callId, remoteUserId, isVideo, true)

            // Create offer
            NativeWebRTC.createOffer(handle)

            // Wait for offer creation callback, then send via signaling
        }
    }

    fun acceptIncomingCall(callId: String, localSurface: Surface?) {
        val incoming = _callState.value as? CallState.Incoming ?: return
        
        scope.launch {
            _callState.value = CallState.Connecting

            val handle = NativeWebRTC.createPeerConnection("stun:stun.l.google.com:19302")
            
            NativeWebRTC.addAudioTrack(handle)
            if (incoming.isVideo && localSurface != null) {
                NativeWebRTC.addVideoTrack(handle, localSurface)
            }

            currentCall = ActiveCall(handle, callId, incoming.callerId, incoming.isVideo, false)

            signaling?.invoke("AcceptCall", callId)
        }
    }

    fun rejectCall(callId: String) {
        signaling?.invoke("RejectCall", callId)
        _callState.value = CallState.Idle
    }

    fun endCall() {
        currentCall?.let { call ->
            NativeWebRTC.close(call.handle)
            signaling?.invoke("EndCall", call.callId)
        }
        currentCall = null
        _callState.value = CallState.Idle
        _remoteVideoSurface.value = null
    }

    fun toggleAudio(): Boolean {
        currentCall?.let {
            val newState = !isAudioEnabled
            NativeWebRTC.setAudioEnabled(it.handle, newState)
            isAudioEnabled = newState
            return newState
        }
        return false
    }

    fun toggleVideo(): Boolean {
        currentCall?.let {
            val newState = !isVideoEnabled
            NativeWebRTC.setVideoEnabled(it.handle, newState)
            isVideoEnabled = newState
            return newState
        }
        return false
    }

    private var isAudioEnabled = true
    private var isVideoEnabled = true

    // Called from native via JNI
    internal companion object {
        private val managers = ConcurrentHashMap<Long, WebRTCManager>()

        fun register(handle: Long, manager: WebRTCManager) {
            managers[handle] = manager
        }

        fun onIceCandidateNative(handle: Long, sdpMid: String, index: Int, candidate: String) {
            managers[handle]?.scope?.launch {
                managers[handle]?.currentCall?.let { call ->
                    managers[handle]?.signaling?.invoke(
                        "SendIceCandidate",
                        call.callId, sdpMid, index, candidate
                    )
                }
            }
        }

        fun onOfferCreatedNative(handle: Long, sdp: String) {
            managers[handle]?.scope?.launch {
                managers[handle]?.currentCall?.let { call ->
                    NativeWebRTC.setLocalDescription(handle, "offer", sdp)
                    managers[handle]?.signaling?.invoke(
                        "InitiateCall",
                        call.callId, call.remoteUserId, call.isVideo, sdp
                    )
                }
            }
        }

        fun onAnswerCreatedNative(handle: Long, sdp: String) {
            managers[handle]?.scope?.launch {
                managers[handle]?.currentCall?.let { call ->
                    NativeWebRTC.setLocalDescription(handle, "answer", sdp)
                    managers[handle]?.signaling?.invoke(
                        "SendAnswer",
                        call.callId, sdp
                    )
                }
            }
        }

        fun onConnectionChangeNative(handle: Long, state: Int) {
            val callState = when (state) {
                2 -> CallState.Connected
                3, 4, 5 -> CallState.Ended
                else -> return
            }
            managers[handle]?.scope?.launch(Dispatchers.Main) {
                managers[handle]?._callState?.value = callState
            }
        }

        fun onRemoteVideoTrackNative(handle: Long) {
            // Remote video track added - UI will handle rendering
        }
    }

    private fun drainIceCandidates(handle: Long) {
        pendingIceCandidates[handle]?.forEach { ice ->
            NativeWebRTC.addIceCandidate(handle, ice.sdpMid, ice.sdpMLineIndex, ice.sdp)
        }
        pendingIceCandidates.remove(handle)
    }

    private fun generateCallId(): String = 
        "call_${System.currentTimeMillis()}_${(0..9999).random()}"

    private fun getCurrentUserId(): String = 
        context.getSharedPreferences("auth", Context.MODE_PRIVATE)
            .getString("user_id", "") ?: ""

    private fun getDeviceIP(): String {
        // Get actual device IP for Lyfron threat analysis
        return try {
            java.net.NetworkInterface.getNetworkInterfaces().toList()
                .flatMap { it.inetAddresses.toList() }
                .firstOrNull { !it.isLoopbackAddress && it is java.net.Inet4Address }
                ?.hostAddress ?: "127.0.0.1"
        } catch (e: Exception) { "127.0.0.1" }
    }

    override fun onDestroy(owner: LifecycleOwner) {
        endCall()
        signaling?.stop()
        scope.cancel()
        super.onDestroy(owner)
    }
}

sealed class CallState {
    object Idle : CallState()
    data class Incoming(val callId: String, val callerId: String, val callerName: String, val isVideo: Boolean) : CallState()
    object Connecting : CallState()
    object Connected : CallState()
    object Ended : CallState()
    data class Error(val message: String) : CallState()
}

data class IceCandidate(val sdpMid: String, val sdpMLineIndex: Int, val sdp: String)