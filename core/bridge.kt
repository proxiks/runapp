class LyfronBridge {
    init {
        System.loadLibrary("lyfron")
    }
    
    external fun checkThreat(userID: String): String
    
    fun analyzeUser(userID: String): ThreatReport {
        val json = checkThreat(userID)
        return parseThreatJson(json)
    }
}