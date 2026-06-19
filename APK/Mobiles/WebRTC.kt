package com.jatin.runapp.webrtc

import android.view.Surface

object NativeWebRTC {
    init {
        System.loadLibrary("lyfron_webrtc")
    }

    @JvmStatic
    external fun initialize()

    @JvmStatic
    external fun createPeerConnection(stunServer: String): Long

    @JvmStatic
    external fun createOffer(handle: Long)

    @JvmStatic
    external fun createAnswer(handle: Long)

    @JvmStatic
    external fun setRemoteDescription(handle: Long, type: String, sdp: String)

    @JvmStatic
    external fun setLocalDescription(handle: Long, type: String, sdp: String)

    @JvmStatic
    external fun addIceCandidate(handle: Long, sdpMid: String, index: Int, candidate: String)

    @JvmStatic
    external fun addAudioTrack(handle: Long)

    @JvmStatic
    external fun addVideoTrack(handle: Long, surface: Surface)

    @JvmStatic
    external fun setAudioEnabled(handle: Long, enabled: Boolean)

    @JvmStatic
    external fun setVideoEnabled(handle: Long, enabled: Boolean)

    @JvmStatic
    external fun close(handle: Long)

    // Called from C++
    @JvmStatic
    fun onIceCandidate(handle: Long, sdpMid: String, index: Int, candidate: String) {
        WebRTCManager.onIceCandidateNative(handle, sdpMid, index, candidate)
    }

    @JvmStatic
    fun onOfferCreated(handle: Long, sdp: String) {
        WebRTCManager.onOfferCreatedNative(handle, sdp)
    }

    @JvmStatic
    fun onAnswerCreated(handle: Long, sdp: String) {
        WebRTCManager.onAnswerCreatedNative(handle, sdp)
    }

    @JvmStatic
    fun onConnectionChange(handle: Long, state: Int) {
        WebRTCManager.onConnectionChangeNative(handle, state)
    }

    @JvmStatic
    fun onRemoteVideoTrack(handle: Long) {
        WebRTCManager.onRemoteVideoTrackNative(handle)
    }
}