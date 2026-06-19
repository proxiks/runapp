package com.jatin.runapp.screens

import android.view.Surface
import android.view.SurfaceView
import android.view.TextureView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavHostController
import com.jatin.runapp.webrtc.CallState
import com.jatin.runapp.webrtc.WebRTCManager
import kotlinx.coroutines.delay

@Composable
fun CallScreen(
    navController: NavHostController,
    webRTCManager: WebRTCManager,
    remoteUserId: String,
    isVideo: Boolean,
    isIncoming: Boolean = false,
    incomingCallId: String? = null,
    callerName: String = ""
) {
    val context = LocalContext.current
    val callState by webRTCManager.callState.collectAsStateWithLifecycle()
    
    var localSurface by remember { mutableStateOf<Surface?>(null) }
    var remoteSurface by remember { mutableStateOf<Surface?>(null) }
    var callDuration by remember { mutableStateOf(0) }
    var isMuted by remember { mutableStateOf(false) }
    var isCameraOff by remember { mutableStateOf(false) }

    // Timer for connected calls
    LaunchedEffect(callState) {
        if (callState is CallState.Connected) {
            while (true) {
                delay(1000)
                callDuration++
            }
        }
    }

    // Auto-navigate back when call ends
    LaunchedEffect(callState) {
        if (callState is CallState.Idle || callState is CallState.Ended) {
            delay(500)
            navController.popBackStack()
        }
    }

    Box(modifier = Modifier.fillMaxSize()) {
        when (val state = callState) {
            is CallState.Incoming -> IncomingCallUI(
                callerName = state.callerName,
                isVideo = state.isVideo,
                onAccept = {
                    webRTCManager.acceptIncomingCall(state.callId, localSurface)
                },
                onReject = {
                    webRTCManager.rejectCall(state.callId)
                }
            )

            is CallState.Connecting -> ConnectingUI(callerName)

            is CallState.Connected -> ConnectedCallUI(
                isVideo = isVideo,
                callDuration = callDuration,
                isMuted = isMuted,
                isCameraOff = isCameraOff,
                localSurface = localSurface,
                remoteSurface = remoteSurface,
                onToggleMute = {
                    isMuted = !webRTCManager.toggleAudio()
                },
                onToggleCamera = {
                    isCameraOff = !webRTCManager.toggleVideo()
                },
                onEndCall = {
                    webRTCManager.endCall()
                },
                onFlipCamera = {
                    // Switch between front/back camera
                }
            )

            else -> {}
        }
    }

    // Initialize local video surface
    if (isVideo && callState !is CallState.Idle) {
        AndroidView(
            factory = { ctx ->
                SurfaceView(ctx).apply {
                    holder.addCallback(object : android.view.SurfaceHolder.Callback {
                        override fun surfaceCreated(holder: android.view.SurfaceHolder) {
                            localSurface = holder.surface
                            if (!isIncoming) {
                                webRTCManager.startCall(remoteUserId, true, holder.surface)
                            }
                        }
                        override fun surfaceChanged(holder: android.view.SurfaceHolder, format: Int, width: Int, height: Int) {}
                        override fun surfaceDestroyed(holder: android.view.SurfaceHolder) {}
                    })
                }
            },
            modifier = if (callState is CallState.Connected) {
                Modifier
                    .size(120.dp, 160.dp)
                    .align(Alignment.TopEnd)
                    .padding(16.dp)
                    .clip(MaterialTheme.shapes.small)
            } else {
                Modifier.fillMaxSize()
            }
        )
    } else if (!isIncoming && callState is CallState.Idle) {
        // Audio call - no surface needed
        LaunchedEffect(Unit) {
            webRTCManager.startCall(remoteUserId, false, null)
        }
    }
}

@Composable
private fun IncomingCallUI(
    callerName: String,
    isVideo: Boolean,
    onAccept: () -> Unit,
    onReject: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1A1A2E)),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        // Caller avatar
        Surface(
            shape = CircleShape,
            color = MaterialTheme.colorScheme.primary,
            modifier = Modifier.size(120.dp)
        ) {
            Box(contentAlignment = Alignment.Center) {
                Text(
                    text = callerName.first().toString(),
                    fontSize = 48.sp,
                    color = Color.White,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        Spacer(modifier = Modifier.height(32.dp))

        Text(
            text = callerName,
            fontSize = 28.sp,
            color = Color.White,
            fontWeight = FontWeight.Bold
        )

        Text(
            text = if (isVideo) "Incoming video call..." else "Incoming voice call...",
            fontSize = 16.sp,
            color = Color.Gray
        )

        Spacer(modifier = Modifier.height(64.dp))

        Row(
            horizontalArrangement = Arrangement.spacedBy(48.dp)
        ) {
            // Decline button
            FilledIconButton(
                onClick = onReject,
                modifier = Modifier.size(72.dp),
                colors = IconButtonDefaults.filledIconButtonColors(
                    containerColor = Color(0xFFE53935)
                )
            ) {
                Icon(
                    Icons.Default.CallEnd,
                    contentDescription = "Decline",
                    modifier = Modifier.size(32.dp),
                    tint = Color.White
                )
            }

            // Accept button
            FilledIconButton(
                onClick = onAccept,
                modifier = Modifier.size(72.dp),
                colors = IconButtonDefaults.filledIconButtonColors(
                    containerColor = Color(0xFF43A047)
                )
            ) {
                Icon(
                    if (isVideo) Icons.Default.Videocam else Icons.Default.Call,
                    contentDescription = "Accept",
                    modifier = Modifier.size(32.dp),
                    tint = Color.White
                )
            }
        }
    }
}

@Composable
private fun ConnectingUI(callerName: String) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1A1A2E)),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        CircularProgressIndicator(
            color = MaterialTheme.colorScheme.primary,
            modifier = Modifier.size(64.dp)
        )

        Spacer(modifier = Modifier.height(24.dp))

        Text(
            text = "Calling $callerName...",
            fontSize = 20.sp,
            color = Color.White
        )
    }
}

@Composable
private fun ConnectedCallUI(
    isVideo: Boolean,
    callDuration: Int,
    isMuted: Boolean,
    isCameraOff: Boolean,
    localSurface: Surface?,
    remoteSurface: Surface?,
    onToggleMute: () -> Unit,
    onToggleCamera: () -> Unit,
    onEndCall: () -> Unit,
    onFlipCamera: () -> Unit
) {
    Box(modifier = Modifier.fillMaxSize()) {
        // Remote video (full screen)
        if (isVideo && remoteSurface != null) {
            AndroidView(
                factory = { ctx ->
                    TextureView(ctx).apply {
                        surfaceTextureListener = object : TextureView.SurfaceTextureListener {
                            override fun onSurfaceTextureAvailable(surface: SurfaceTexture, width: Int, height: Int) {
                                // Bind remote video stream
                            }
                            override fun onSurfaceTextureSizeChanged(surface: SurfaceTexture, width: Int, height: Int) {}
                            override fun onSurfaceTextureDestroyed(surface: SurfaceTexture) = true
                            override fun onSurfaceTextureUpdated(surface: SurfaceTexture) {}
                        }
                    }
                },
                modifier = Modifier.fillMaxSize()
            )
        } else {
            // Audio call background
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(Color(0xFF1A1A2E)),
                contentAlignment = Alignment.Center
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Surface(
                        shape = CircleShape,
                        color = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.size(100.dp)
                    ) {
                        Box(contentAlignment = Alignment.Center) {
                            Text(
                                text = "J", // Dynamic
                                fontSize = 40.sp,
                                color = Color.White
                            )
                        }
                    }
                }
            }
        }

        // Call duration
        Text(
            text = formatDuration(callDuration),
            modifier = Modifier
                .align(Alignment.TopCenter)
                .padding(top = 48.dp),
            color = Color.White,
            fontSize = 16.sp
        )

        // Control bar
        Row(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .padding(bottom = 48.dp)
                .fillMaxWidth()
                .padding(horizontal = 32.dp),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            // Mute
            ControlButton(
                icon = if (isMuted) Icons.Default.MicOff else Icons.Default.Mic,
                label = if (isMuted) "Unmute" else "Mute",
                isActive = !isMuted,
                onClick = onToggleMute
            )

            // Video toggle (video calls only)
            if (isVideo) {
                ControlButton(
                    icon = if (isCameraOff) Icons.Default.VideocamOff else Icons.Default.Videocam,
                    label = if (isCameraOff) "Camera On" else "Camera Off",
                    isActive = !isCameraOff,
                    onClick = onToggleCamera
                )

                // Flip camera
                ControlButton(
                    icon = Icons.Default.Cameraswitch,
                    label = "Flip",
                    isActive = true,
                    onClick = onFlipCamera
                )
            }

            // Speaker (audio calls)
            if (!isVideo) {
                ControlButton(
                    icon = Icons.Default.VolumeUp,
                    label = "Speaker",
                    isActive = true,
                    onClick = { /* Toggle speaker */ }
                )
            }

            // End call
            FilledIconButton(
                onClick = onEndCall,
                modifier = Modifier.size(64.dp),
                colors = IconButtonDefaults.filledIconButtonColors(
                    containerColor = Color(0xFFE53935)
                )
            ) {
                Icon(
                    Icons.Default.CallEnd,
                    contentDescription = "End Call",
                    tint = Color.White,
                    modifier = Modifier.size(28.dp)
                )
            }
        }
    }
}

@Composable
private fun ControlButton(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
    isActive: Boolean,
    onClick: () -> Unit
) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        FilledIconButton(
            onClick = onClick,
            modifier = Modifier.size(56.dp),
            colors = IconButtonDefaults.filledIconButtonColors(
                containerColor = if (isActive) Color(0xFF3A3A4A) else Color(0xFFE53935)
            )
        ) {
            Icon(
                icon,
                contentDescription = label,
                tint = Color.White,
                modifier = Modifier.size(24.dp)
            )
        }
        Text(
            text = label,
            color = Color.White,
            fontSize = 12.sp,
            modifier = Modifier.padding(top = 4.dp)
        )
    }
}

private fun formatDuration(seconds: Int): String {
    val mins = seconds / 60
    val secs = seconds % 60
    return String.format("%02d:%02d", mins, secs)
}