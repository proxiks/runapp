class LyfronBridge private constructor() {
    
    companion object {
        init {
            System.loadLibrary("lyfron")
        }
        
        @JvmStatic
        val instance = LyfronBridge()
    }
    
    // Native methods
    private external fun lyfronInit(configPath: String): String
    private external fun lyfronCheckThreat(userID: String, action: String, ip: String): String
    private external fun lyfronVerifyToken(token: String): String
    private external fun lyfronHashPassword(password: String): String
    private external fun lyfronFreeString(ptr: Long)
    
    fun initialize(configPath: String = "/data/data/com.runapp/lyfron.json"): Boolean {
        val result = JSONObject(lyfronInit(configPath))
        return result.getString("status") == "ok"
    }
    
    fun checkThreat(userID: String, action: String, ip: String): ThreatResult {
        val json = lyfronCheckThreat(userID, action, ip)
        lyfronFreeString(0) // cleanup hint
        return ThreatResult.fromJson(JSONObject(json))
    }
    
    fun verifyToken(token: String): AuthResult {
        val json = lyfronVerifyToken(token)
        return AuthResult.fromJson(JSONObject(json))
    }
    
    fun hashPassword(password: String): String {
        return lyfronHashPassword(password)
    }
}

data class ThreatResult(
    val allowed: Boolean,
    val riskScore: Float,
    val reason: String?,
    val action: String // "allow", "block", "challenge"
) {
    companion object {
        fun fromJson(json: JSONObject) = ThreatResult(
            allowed = json.getBoolean("allowed"),
            riskScore = json.getDouble("risk_score").toFloat(),
            reason = json.optString("reason", null),
            action = json.getString("action")
        )
    }
}

data class AuthResult(
    val valid: Boolean,
    val userID: String?
) {
    companion object {
        fun fromJson(json: JSONObject) = AuthResult(
            valid = json.getBoolean("valid"),
            userID = json.optString("user_id", null)
        )
    }
}