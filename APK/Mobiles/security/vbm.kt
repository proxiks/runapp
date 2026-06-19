class VerifiedBadgeManager {
    
    private val lyfron = LyfronBridge.instance
    
    fun purchaseVerified(userID: String, token: String): Boolean {
        // Lyfron validates payment + fraud check in one shot
        val threat = lyfron.checkThreat(
            userID = userID,
            action = "purchase:verified",
            ip = getDeviceIP()
        )
        
        if (!threat.allowed) {
            Log.w("LYFRON", "Blocked verified purchase: ${threat.reason}")
            return false
        }
        
        // Verify payment token through Lyfron
        val auth = lyfron.verifyToken(token)
        return auth.valid && auth.userID == userID
    }
    
    fun isVerified(userID: String): Boolean {
        // Local check via Lyfron cached state
        return lyfron.checkThreat(
            userID = userID,
            action = "badge:check",
            ip = "127.0.0.1"
        ).allowed
    }
}
