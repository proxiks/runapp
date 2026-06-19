package com.jatin.runapp.data

import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST

interface AIMemeService {
    @POST("chat/completions")
    suspend fun generateMeme(
        @Header("Authorization") auth: String = "Bearer ${AIMemeConfig.XAI_API_KEY}",
        @Body request: GrokRequest
    ): Response<GrokResponse>

    companion object {
        fun create(): AIMemeService {
            return Retrofit.Builder()
                .baseUrl(AIMemeConfig.BASE_URL)
                .addConverterFactory(GsonConverterFactory.create())
                .build()
                .create(AIMemeService::class.java)
        }
    }
}

data class GrokRequest(
    val model: String = AIMemeConfig.MODEL,
    val messages: List<GrokMessage>,
    val temperature: Double = 0.9,
    val max_tokens: Int = 150
)

data class GrokMessage(
    val role: String = "user",
    val content: String
)

data class GrokResponse(
    val choices: List<GrokChoice>
)

data class GrokChoice(
    val message: GrokMessageContent
)

data class GrokMessageContent(
    val content: String
)
