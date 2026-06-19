// In CallViewModel.kt
fun startCall(userId: String, isVideo: Boolean) {
    viewModelScope.launch {
        // Check if user is follower (CallHub validates this)
        signaling.invoke("InviteToCall", callSessionId, userId)
    }
}

fun acceptInvite(inviteId: Int) {
    signaling.invoke("AcceptCallInvite", inviteId)
    // Initialize WebRTC, create answer
}