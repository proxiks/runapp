package com.jatin.runapp.data

data class User(
    val id: String,
    val name: String,
    val email: String,
    val password: String = "",
    val bio: String = "",
    val avatarUrl: String = "",
    val followersCount: Int = 0,
    val followingCount: Int = 0,
    val friendsCount: Int = 0,
    val isFollowing: Boolean = false,
    val isFriend: Boolean = false,
    val friendRequestSent: Boolean = false,
    val friendRequestReceived: Boolean = false
)

data class UserSettings(
    val id: String,
    val name: String,
    val username: String,
    val email: String,
    val phoneNumber: String?,
    val bio: String,
    val avatarUrl: String,
    val isEmailVerified: Boolean,
    val isPhoneVerified: Boolean,
    val twoFactorEnabled: Boolean,
    val twoFactorMethod: TwoFactorMethod?,
    val privacyLevel: PrivacyLevel,
    val notificationsEnabled: Boolean,
    val darkMode: Boolean
)

enum class TwoFactorMethod {
    SMS, EMAIL, AUTHENTICATOR
}

enum class PrivacyLevel {
    PUBLIC, FRIENDS, PRIVATE
}

data class Post(
    val id: String,
    val authorId: String,
    val authorName: String,
    val authorAvatar: String = "",
    val content: String,
    val imageUrl: String? = null,
    val videoUrl: String? = null,
    val likes: Int = 0,
    val comments: Int = 0,
    val shares: Int = 0,
    val isLiked: Boolean = false,
    val timestamp: String,
    val isFriendPost: Boolean = false
)

data class Comment(
    val id: String,
    val authorId: String,
    val authorName: String,
    val content: String,
    val timestamp: String,
    val likes: Int = 0
)

data class FriendRequest(
    val id: String,
    val fromUserId: String,
    val fromUserName: String,
    val fromUserAvatar: String = "",
    val timestamp: String,
    val status: RequestStatus = RequestStatus.PENDING
)

enum class RequestStatus {
    PENDING, ACCEPTED, REJECTED
}

data class Notification(
    val id: String,
    val type: NotificationType,
    val fromUserId: String,
    val fromUserName: String,
    val fromUserAvatar: String = "",
    val content: String,
    val timestamp: String,
    val isRead: Boolean = false,
    val postId: String? = null
)

enum class NotificationType {
    LIKE, COMMENT, FOLLOW, FRIEND_REQUEST, FRIEND_ACCEPT, SHARE, MENTION
}

data class Message(
    val id: String,
    val senderId: String,
    val receiverId: String,
    val content: String,
    val createdAt: String,
    val isRead: Boolean = false
)

data class Video(
    val id: String,
    val title: String,
    val authorId: String,
    val authorName: String,
    val authorAvatar: String = "",
    val thumbnailUrl: String = "",
    val videoUrl: String,
    val views: Int = 0,
    val likes: Int = 0,
    val duration: String,
    val timestamp: String,
    val isLiked: Boolean = false
)

data class ValidationResult(
    val isValid: Boolean,
    val errorMessage: String? = null
)