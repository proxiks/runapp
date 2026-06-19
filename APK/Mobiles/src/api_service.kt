package com.jatin.runapp.data

import retrofit2.Response
import retrofit2.http.*
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

interface ApiService {
    // Auth
    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>
    
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>
    
    // Posts
    @GET("feed")
    suspend fun getFeed(): Response<List<Post>>
    
    @POST("posts")
    suspend fun createPost(@Body request: CreatePostRequest): Response<PostResponse>
    
    @POST("posts/{id}/like")
    suspend fun likePost(@Path("id") postId: String): Response<LikeResponse>
    
    @DELETE("posts/{id}")
    suspend fun deletePost(@Path("id") postId: String): Response<Unit>
    
    // Comments
    @GET("posts/{id}/comments")
    suspend fun getComments(@Path("id") postId: String): Response<List<Comment>>
    
    @POST("posts/{id}/comment")
    suspend fun addComment(
        @Path("id") postId: String,
        @Body request: CommentRequest
    ): Response<Comment>
    
    // Messages
    @GET("messages/{userId}")
    suspend fun getMessages(@Path("userId") userId: String): Response<List<Message>>
    
    @POST("messages")
    suspend fun sendMessage(@Body request: SendMessageRequest): Response<Message>
    
    // Users
    @GET("users/search")
    suspend fun searchUsers(@Query("q") query: String): Response<List<User>>
    
    @POST("users/{id}/follow")
    suspend fun followUser(@Path("id") userId: String): Response<Unit>
    
    @POST("users/{id}/unfollow")
    suspend fun unfollowUser(@Path("id") userId: String): Response<Unit>
    
    // Friends
    @POST("users/{id}/friend-request")
    suspend fun sendFriendRequest(@Path("id") userId: String): Response<Unit>
    
    @POST("friend-requests/{id}/accept")
    suspend fun acceptFriendRequest(@Path("id") requestId: String): Response<Unit>
    
    // Notifications
    @GET("notifications")
    suspend fun getNotifications(): Response<List<Notification>>
    
    @POST("notifications/{id}/read")
    suspend fun markNotificationRead(@Path("id") notificationId: String): Response<Unit>
    
    // Upload
    @Multipart
    @POST("upload")
    suspend fun uploadFile(
        @Part file: okhttp3.MultipartBody.Part
    ): Response<UploadResponse>
    
    companion object {
        fun create(): ApiService {
            return Retrofit.Builder()
                .baseUrl("http://YOUR_SERVER_IP:8080/api/")
                .addConverterFactory(GsonConverterFactory.create())
                .build()
                .create(ApiService::class.java)
        }
    }
}

// Request/Response data classes
data class RegisterRequest(val name: String, val email: String, val password: String)
data class LoginRequest(val email: String, val password: String)
data class AuthResponse(val token: String, val user: User)
data class CreatePostRequest(val content: String, val imageUrl: String?, val videoUrl: String?)
data class PostResponse(val id: String)
data class LikeResponse(val likes: Int, val isLiked: Boolean)
data class CommentRequest(val content: String)
data class Comment(val id: String, val authorId: String, val authorName: String, val content: String, val timestamp: String)
data class SendMessageRequest(val receiver_id: String, val content: String)
data class UploadResponse(val url: String)