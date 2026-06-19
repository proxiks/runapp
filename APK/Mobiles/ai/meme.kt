package com.jatin.runapp.data

class MemeRepository(private val api: AIMemeService = AIMemeService.create()) {
    
    suspend fun generateMemeText(topic: String, template: String): Result<Pair<String, String>> {
        return try {
            val prompt = buildMemePrompt(topic, template)
            val request = GrokRequest(messages = listOf(GrokMessage(content = prompt)))
            
            val response = api.generateMeme(request = request)
            
            if (response.isSuccessful) {
                val content = response.body()?.choices?.firstOrNull()?.message?.content ?: ""
                val (top, bottom) = parseMemeResponse(content)
                Result.success(top to bottom)
            } else {
                Result.failure(Exception("API Error: ${response.code()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    private fun buildMemePrompt(topic: String, template: String): String {
        return """
            Create a funny meme about "$topic" using the "$template" format.
            Return ONLY in this exact format:
            TOP: [top text]
            BOTTOM: [bottom text]
            
            Make it short, punchy, and internet-humor style. No explanations.
        """.trimIndent()
    }
    
    private fun parseMemeResponse(response: String): Pair<String, String> {
        val top = response.lines()
            .find { it.startsWith("TOP:") }
            ?.removePrefix("TOP:")
            ?.trim() ?: "When you $topic"
            
        val bottom = response.lines()
            .find { it.startsWith("BOTTOM:") }
            ?.removePrefix("BOTTOM:")
            ?.trim() ?: "But then reality hits"
            
        return top to bottom
    }
}
