package com.jatin.runapp.data

import com.jatin.runapp.utils.ValidationUtils
import kotlinx.coroutines.delay
import java.util.UUID

object MockServer {
    private val users = mutableListOf<User>()
    private val userSettings = mutableMapOf<String, UserSettings>()
    private val posts = mutableListOf<Post>()
    private val friendRequests = mutableListOf<FriendRequest>()
    private val notifications = mutableListOf<Notification>()
    private val videos = mutableListOf<Video>()
    private val messages = mutableListOf<Message>()
    private var currentUserId: String? = null
    private val pending2FACodes = mutableMapOf<String, String>()

    init {
        val jatinId = "user_jatin_001"
        currentUserId = jatinId
        
        users.add(User(
            id = jatinId,
            name = "Jatin",
            email = "jatin@email.com",
            password = "hashed_password123",
            bio = "Building JatinBook 🚀",
            avatarUrl = "",
            followersCount = 234,
            followingCount = 156,
            friendsCount = 89
        ))
        
        userSettings[jatinId] = UserSettings(
            id = jatinId,
            name = "Jatin",
            username = "jatin",
            email = "jatin@email.com",
            phoneNumber = "+91 98765 43210",
            bio = "Building JatinBook 🚀",
            avatarUrl = "",
            isEmailVerified = true,
            isPhoneVerified = false,
            twoFactorEnabled = false,
            twoFactorMethod = null,
            privacyLevel = PrivacyLevel.PUBLIC,
            notificationsEnabled = true,
            darkMode = false
        )

        users.addAll(listOf(
            User("user_2", "Rahul", "rahul@email.com", bio = "Tech enthusiast 💻", followersCount = 1200, followingCount = 450, friendsCount = 300),
            User("user_3", "Priya", "priya@email.com", bio = "Travel blogger ✈️", followersCount = 5600, followingCount = 230, friendsCount = 180),
            User("user_4", "Amit", "amit@email.com", bio = "Foodie 🍕", followersCount = 890, followingCount = 670, friendsCount = 420),
            User("user_5", "Sneha", "sneha@email.com", bio = "Gamer 🎮", followersCount = 3400, followingCount = 120, friendsCount = 95),
            User("user_6", "Vikram", "vikram@email.com", bio = "Photographer 📸", followersCount = 2100, followingCount = 890, friendsCount = 250)
        ))
        
        posts.addAll(listOf(
            Post("post_1", "user_2", "Rahul", content = "Just launched my new app! 🚀", likes = 42, comments = 5, timestamp = "2h ago", isFriendPost = true),
            Post("post_2", "user_3", "Priya", content = "Beautiful sunset in Goa! 🌅", imageUrl = "sunset", likes = 128, comments = 12, timestamp = "4h ago", isFriendPost = true),
            Post("post_3", "user_1", "Jatin", content = "Working on JatinBook features 💻", likes = 67, comments = 8, timestamp = "6h ago", isFriendPost = true),
            Post("post_4", "user_4", "Amit", content = "Best pizza in town! 🍕", likes = 234, comments = 23, timestamp = "8h ago", isFriendPost = false),
            Post("post_5", "user_5", "Sneha", content = "New gaming setup! 🎮", likes = 89, comments = 15, timestamp = "10h ago", isFriendPost = true),
            Post("post_6", "user_6", "Vikram", content = "Check out my new photos 📸", likes = 156, comments = 31, timestamp = "12h ago", isFriendPost = false)
        ))
        
        videos.addAll(listOf(
            Video("vid_1", "My First Vlog", "user_2", "Rahul", videoUrl = "https://example.com/v1", views = 1200, likes = 89, duration = "3:45", timestamp = "1d ago"),
            Video("vid_2", "Coding Tutorial", "user_3", "Priya", videoUrl = "https://example.com/v2", views = 5600, likes = 234, duration = "12:30", timestamp = "2d ago"),
            Video("vid_3", "Travel Diaries", "user_4", "Amit", videoUrl = "https://example.com/v3", views = 890, likes = 67, duration = "8:15", timestamp = "3d ago"),
            Video("vid_4", "Gaming Highlights", "user_5", "Sneha", videoUrl = "https://example.com/v4", views = 10000, likes = 567, duration = "15:00", timestamp = "4d ago")
        ))
    }

    // ========== AUTH ==========
    fun login(email: String, password: String): Result<User> {
        val user = users.find { it.email == email }
        return if (user != null && verifyPassword(password, user.password)) {
            currentUserId = user.id
            Result.success(user)
        } else {
            Result.failure(Exception("Invalid email or password"))
        }
    }
    
    fun signup(name: String, email: String, password: String): Result<User> {
        if (users.any { it.email == email }) {
            return Result.failure(Exception("Email already exists"))
        }
        val newUser = User(
            id = "user_${UUID.randomUUID().toString().take(8)}",
            name = name,
            email = email,
            password = hashPassword(password)
        )
        users.add(newUser)
        currentUserId = newUser.id
        
        userSettings[newUser.id] = UserSettings(
            id = newUser.id,
            name = name,
            username = name.lowercase().replace(" ", ""),
            email = email,
            phoneNumber = null,
            bio = "",
            avatarUrl = "",
            isEmailVerified = false,
            isPhoneVerified = false,
            twoFactorEnabled = false,
            twoFactorMethod = null,
            privacyLevel = PrivacyLevel.PUBLIC,
            notificationsEnabled = true,
            darkMode = false
        )
        
        return Result.success(newUser)
    }
    
    fun getCurrentUser(): User? = users.find { it.id == currentUserId }
    fun getCurrentUserSettings(): UserSettings? = userSettings[currentUserId]

    // ========== SETTINGS ==========
    fun updateProfile(name: String? = null, username: String? = null, bio: String? = null): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        name?.let {
            val validation = ValidationUtils.validateName(it)
            if (!validation.isValid) return Result.failure(Exception(validation.errorMessage))
        }
        
        username?.let {
            val validation = ValidationUtils.validateUsername(it)
            if (!validation.isValid) return Result.failure(Exception(validation.errorMessage))
            if (userSettings.values.any { s -> s.username == it && s.id != userId }) {
                return Result.failure(Exception("Username already taken"))
            }
        }
        
        bio?.let {
            val validation = ValidationUtils.validateBio(it)
            if (!validation.isValid) return Result.failure(Exception(validation.errorMessage))
        }
        
        val updated = settings.copy(
            name = name ?: settings.name,
            username = username ?: settings.username,
            bio = bio ?: settings.bio
        )
        userSettings[userId] = updated
        
        val userIndex = users.indexOfFirst { it.id == userId }
        if (userIndex != -1 && name != null) {
            users[userIndex] = users[userIndex].copy(name = name, bio = bio ?: users[userIndex].bio)
        }
        
        return Result.success(updated)
    }

    fun updateEmail(newEmail: String, currentPassword: String): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val user = users.find { it.id == userId } ?: return Result.failure(Exception("User not found"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        if (!verifyPassword(currentPassword, user.password)) {
            return Result.failure(Exception("Incorrect password"))
        }
        
        val validation = ValidationUtils.validateEmail(newEmail)
        if (!validation.isValid) return Result.failure(Exception(validation.errorMessage))
        
        if (users.any { it.email == newEmail && it.id != userId }) {
            return Result.failure(Exception("Email already in use"))
        }
        
        val updatedSettings = settings.copy(email = newEmail, isEmailVerified = false)
        userSettings[userId] = updatedSettings
        
        val userIndex = users.indexOf(user)
        users[userIndex] = user.copy(email = newEmail)
        
        return Result.success(updatedSettings)
    }

    fun updatePhoneNumber(phoneNumber: String, currentPassword: String): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val user = users.find { it.id == userId } ?: return Result.failure(Exception("User not found"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        if (!verifyPassword(currentPassword, user.password)) {
            return Result.failure(Exception("Incorrect password"))
        }
        
        val validation = ValidationUtils.validatePhoneNumber(phoneNumber)
        if (!validation.isValid) return Result.failure(Exception(validation.errorMessage))
        
        if (userSettings.values.any { it.phoneNumber == phoneNumber && it.id != userId }) {
            return Result.failure(Exception("Phone number already in use"))
        }
        
        val updated = settings.copy(phoneNumber = phoneNumber, isPhoneVerified = false)
        userSettings[userId] = updated
        
        return Result.success(updated)
    }

    fun changePassword(currentPassword: String, newPassword: String, confirmPassword: String): Result<Unit> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val user = users.find { it.id == userId } ?: return Result.failure(Exception("User not found"))
        
        if (!verifyPassword(currentPassword, user.password)) {
            return Result.failure(Exception("Current password is incorrect"))
        }
        
        val validation = ValidationUtils.validatePassword(newPassword)
        if (!validation.isValid) return Result.failure(Exception(validation.errorMessage))
        
        val matchValidation = ValidationUtils.validatePasswordMatch(newPassword, confirmPassword)
        if (!matchValidation.isValid) return Result.failure(Exception(matchValidation.errorMessage))
        
        val userIndex = users.indexOf(user)
        users[userIndex] = user.copy(password = hashPassword(newPassword))
        
        return Result.success(Unit)
    }

    fun updateAvatar(imageUrl: String): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        val updated = settings.copy(avatarUrl = imageUrl)
        userSettings[userId] = updated
        
        val userIndex = users.indexOfFirst { it.id == userId }
        if (userIndex != -1) {
            users[userIndex] = users[userIndex].copy(avatarUrl = imageUrl)
        }
        
        return Result.success(updated)
    }

    // ========== 2FA ==========
    fun enable2FA(method: TwoFactorMethod, password: String): Result<String> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val user = users.find { it.id == userId } ?: return Result.failure(Exception("User not found"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        if (!verifyPassword(password, user.password)) {
            return Result.failure(Exception("Incorrect password"))
        }
        
        if (settings.twoFactorEnabled) {
            return Result.failure(Exception("2FA is already enabled"))
        }
        
        val code = generate2FACode()
        pending2FACodes[userId] = code
        
        return Result.success(code)
    }

    fun verify2FASetup(code: String): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        val expectedCode = pending2FACodes[userId]
        if (expectedCode == null || expectedCode != code) {
            return Result.failure(Exception("Invalid verification code"))
        }
        
        val updated = settings.copy(twoFactorEnabled = true, twoFactorMethod = TwoFactorMethod.SMS)
        userSettings[userId] = updated
        pending2FACodes.remove(userId)
        
        return Result.success(updated)
    }

    fun disable2FA(password: String): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val user = users.find { it.id == userId } ?: return Result.failure(Exception("User not found"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        if (!verifyPassword(password, user.password)) {
            return Result.failure(Exception("Incorrect password"))
        }
        
        if (!settings.twoFactorEnabled) {
            return Result.failure(Exception("2FA is not enabled"))
        }
        
        val updated = settings.copy(twoFactorEnabled = false, twoFactorMethod = null)
        userSettings[userId] = updated
        
        return Result.success(updated)
    }

    // ========== PRIVACY ==========
    fun updatePrivacyLevel(level: PrivacyLevel): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        val updated = settings.copy(privacyLevel = level)
        userSettings[userId] = updated
        
        return Result.success(updated)
    }

    fun updateNotifications(enabled: Boolean): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        val updated = settings.copy(notificationsEnabled = enabled)
        userSettings[userId] = updated
        
        return Result.success(updated)
    }

    fun updateDarkMode(enabled: Boolean): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        val updated = settings.copy(darkMode = enabled)
        userSettings[userId] = updated
        
        return Result.success(updated)
    }

    // ========== ACCOUNT ACTIONS ==========
    fun logout(): Result<Unit> {
        currentUserId = null
        return Result.success(Unit)
    }

    fun deleteAccount(password: String): Result<Unit> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val user = users.find { it.id == userId } ?: return Result.failure(Exception("User not found"))
        
        if (!verifyPassword(password, user.password)) {
            return Result.failure(Exception("Incorrect password. Account deletion cancelled."))
        }
        
        users.remove(user)
        userSettings.remove(userId)
        currentUserId = null
        
        return Result.success(Unit)
    }

    // ========== VERIFICATION ==========
    fun verifyEmail(code: String): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        if (code != "123456") {
            return Result.failure(Exception("Invalid verification code"))
        }
        
        val updated = settings.copy(isEmailVerified = true)
        userSettings[userId] = updated
        
        return Result.success(updated)
    }

    fun verifyPhone(code: String): Result<UserSettings> {
        val userId = currentUserId ?: return Result.failure(Exception("Not logged in"))
        val settings = userSettings[userId] ?: return Result.failure(Exception("Settings not found"))
        
        if (code != "123456") {
            return Result.failure(Exception("Invalid verification code"))
        }
        
        val updated = settings.copy(isPhoneVerified = true)
        userSettings[userId] = updated
        
        return Result.success(updated)
    }

    // ========== POSTS ==========
    fun getFeed(): List<Post> = posts.sortedByDescending { it.timestamp }
    fun getFriendPosts(): List<Post> = posts.filter { it.isFriendPost }
    
    fun createPost(content: String, imageUrl: String? = null): Post {
        val user = getCurrentUser()!!
        val post = Post(
            id = "post_${UUID.randomUUID().toString().take(8)}",
            authorId = user.id,
            authorName = user.name,
            content = content,
            imageUrl = imageUrl,
            timestamp = "Just now"
        )
        posts.add(0, post)
        return post
    }
    
    fun likePost(postId: String): Post? {
        val post = posts.find { it.id == postId } ?: return null
        val index = posts.indexOf(post)
        val updated = post.copy(
            likes = if (post.isLiked) post.likes - 1 else post.likes + 1,
            isLiked = !post.isLiked
        )
        posts[index] = updated
        return updated
    }

    // ========== FRIENDS ==========
    fun getFriendRequests(): List<FriendRequest> = friendRequests.filter { 
        it.toUserId() == currentUserId && it.status == RequestStatus.PENDING 
    }
    
    fun sendFriendRequest(userId: String): Boolean {
        if (friendRequests.any { it.fromUserId == currentUserId && it.toUserId() == userId }) {
            return false
        }
        val user = users.find { it.id == userId } ?: return false
        friendRequests.add(FriendRequest(
            id = "req_${UUID.randomUUID().toString().take(8)}",
            fromUserId = currentUserId!!,
            fromUserName = getCurrentUser()?.name ?: "",
            timestamp = "Just now"
        ))
        return true
    }
    
    fun acceptFriendRequest(requestId: String): Boolean {
        val request = friendRequests.find { it.id == requestId } ?: return false
        val index = friendRequests.indexOf(request)
        friendRequests[index] = request.copy(status = RequestStatus.ACCEPTED)
        
        val currentUser = users.find { it.id == currentUserId }!!
        val fromUser = users.find { it.id == request.fromUserId }!!
        
        users[users.indexOf(currentUser)] = currentUser.copy(friendsCount = currentUser.friendsCount + 1)
        users[users.indexOf(fromUser)] = fromUser.copy(friendsCount = fromUser.friendsCount + 1)
        
        return true
    }
    
    fun rejectFriendRequest(requestId: String): Boolean {
        val request = friendRequests.find { it.id == requestId } ?: return false
        val index = friendRequests.indexOf(request)
        friendRequests[index] = request.copy(status = RequestStatus.REJECTED)
        return true
    }
    
    fun getFriends(): List<User> {
        val friendIds = friendRequests
            .filter { it.status == RequestStatus.ACCEPTED }
            .map { if (it.fromUserId == currentUserId) it.toUserId() else it.fromUserId }
        return users.filter { it.id in friendIds }
    }

    // ========== FOLLOW ==========
    fun followUser(userId: String): Boolean {
        val userIndex = users.indexOfFirst { it.id == userId }
        if (userIndex == -1) return false
        
        val user = users[userIndex]
        val currentUser = users.find { it.id == currentUserId }!!
        val currentIndex = users.indexOf(currentUser)
        
        users[userIndex] = user.copy(followersCount = user.followersCount + 1, isFollowing = true)
        users[currentIndex] = currentUser.copy(followingCount = currentUser.followingCount + 1)
        
        return true
    }
    
    fun unfollowUser(userId: String): Boolean {
        val userIndex = users.indexOfFirst { it.id == userId }
        if (userIndex == -1) return false
        
        val user = users[userIndex]
        val currentUser = users.find { it.id == currentUserId }!!
        val currentIndex = users.indexOf(currentUser)
        
        users[userIndex] = user.copy(followersCount = user.followersCount - 1, isFollowing = false)
        users[currentIndex] = currentUser.copy(followingCount = currentUser.followingCount - 1)
        
        return true
    }
    
    fun getSuggestedUsers(): List<User> = users.filter { 
        it.id != currentUserId && !it.isFollowing && !it.isFriend 
    }

    // ========== NOTIFICATIONS ==========
    fun getNotifications(): List<Notification> = notifications.sortedByDescending { it.timestamp }
    fun getUnreadCount(): Int = notifications.count { !it.isRead }
    
    fun markNotificationRead(notificationId: String) {
        val index = notifications.indexOfFirst { it.id == notificationId }
        if (index != -1) {
            notifications[index] = notifications[index].copy(isRead = true)
        }
    }
    
    fun markAllRead() {
        notifications.replaceAll { it.copy(isRead = true) } }
    }