package com.jatin.runapp.utils

import android.util.Patterns
import java.util.regex.Pattern

object ValidationUtils {

    // Email validation with real domain check
    fun validateEmail(email: String): ValidationResult {
        if (email.isBlank()) {
            return ValidationResult(false, "Email is required")
        }
        
        if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            return ValidationResult(false, "Invalid email format. Example: user@gmail.com")
        }

        // Check for common fake emails
        val disposableDomains = listOf(
            "tempmail.com", "10minutemail.com", "guerrillamail.com",
            "mailinator.com", "throwawaymail.com", "yopmail.com",
            "fakeemail.com", "test.com", "example.com"
        )
        
        val domain = email.substringAfter("@").lowercase()
        if (disposableDomains.contains(domain)) {
            return ValidationResult(false, "Please use a real email address")
        }

        // Check for obvious fake patterns
        if (email.contains("hjfk") || email.contains("dske") || 
            email.contains("asdf") || email.contains("qwerty") ||
            email.contains("12345") || email.contains("fake")) {
            return ValidationResult(false, "Please enter a valid email address")
        }

        // Check length
        if (email.length > 254) {
            return ValidationResult(false, "Email is too long")
        }

        // Check local part (before @)
        val localPart = email.substringBefore("@")
        if (localPart.length < 1) {
            return ValidationResult(false, "Email must have characters before @")
        }
        if (localPart.length > 64) {
            return ValidationResult(false, "Email username too long")
        }

        return ValidationResult(true)
    }

    // Username validation
    fun validateUsername(username: String): ValidationResult {
        if (username.isBlank()) {
            return ValidationResult(false, "Username is required")
        }
        
        if (username.length < 3) {
            return ValidationResult(false, "Username must be at least 3 characters")
        }
        
        if (username.length > 30) {
            return ValidationResult(false, "Username must be less than 30 characters")
        }

        val usernamePattern = Pattern.compile("^[a-zA-Z0-9_.]+$")
        if (!usernamePattern.matcher(username).matches()) {
            return ValidationResult(false, "Username can only contain letters, numbers, underscores, and dots")
        }

        if (username.startsWith(".") || username.startsWith("_")) {
            return ValidationResult(false, "Username cannot start with . or _")
        }

        // Check for reserved usernames
        val reserved = listOf("admin", "root", "system", "support", "help", "jatinbook", "official")
        if (reserved.contains(username.lowercase())) {
            return ValidationResult(false, "This username is reserved")
        }

        return ValidationResult(true)
    }

    // Name validation
    fun validateName(name: String): ValidationResult {
        if (name.isBlank()) {
            return ValidationResult(false, "Name is required")
        }
        
        if (name.length < 2) {
            return ValidationResult(false, "Name must be at least 2 characters")
        }
        
        if (name.length > 50) {
            return ValidationResult(false, "Name must be less than 50 characters")
        }

        // Allow letters, spaces, hyphens, apostrophes
        val namePattern = Pattern.compile("^[a-zA-Z\\s'-]+$")
        if (!namePattern.matcher(name).matches()) {
            return ValidationResult(false, "Name can only contain letters, spaces, hyphens, and apostrophes")
        }

        return ValidationResult(true)
    }

    // Phone number validation
    fun validatePhoneNumber(phone: String): ValidationResult {
        if (phone.isBlank()) {
            return ValidationResult(false, "Phone number is required")
        }

        // Remove all non-digits
        val digitsOnly = phone.replace(Regex("[^0-9]"), "")
        
        if (digitsOnly.length < 10) {
            return ValidationResult(false, "Phone number must have at least 10 digits")
        }
        
        if (digitsOnly.length > 15) {
            return ValidationResult(false, "Phone number is too long")
        }

        // Basic country code check
        if (!phone.startsWith("+") && digitsOnly.length > 10) {
            return ValidationResult(false, "International numbers must start with +")
        }

        return ValidationResult(true)
    }

    // Password validation
    fun validatePassword(password: String): ValidationResult {
        if (password.isBlank()) {
            return ValidationResult(false, "Password is required")
        }
        
        if (password.length < 8) {
            return ValidationResult(false, "Password must be at least 8 characters")
        }
        
        if (password.length > 128) {
            return ValidationResult(false, "Password is too long")
        }

        var strength = 0
        if (password.any { it.isUpperCase() }) strength++
        if (password.any { it.isLowerCase() }) strength++
        if (password.any { it.isDigit() }) strength++
        if (password.any { !it.isLetterOrDigit() }) strength++

        if (strength < 3) {
            return ValidationResult(false, "Password must contain at least 3 of: uppercase, lowercase, number, special character")
        }

        // Check for common passwords
        val commonPasswords = listOf("password", "123456", "qwerty", "abc123", "password123", "admin123")
        if (commonPasswords.any { password.lowercase().contains(it) }) {
            return ValidationResult(false, "Password is too common. Please choose a stronger password")
        }

        return ValidationResult(true)
    }

    // Confirm password validation
    fun validatePasswordMatch(password: String, confirmPassword: String): ValidationResult {
        if (confirmPassword.isBlank()) {
            return ValidationResult(false, "Please confirm your password")
        }
        if (password != confirmPassword) {
            return ValidationResult(false, "Passwords do not match")
        }
        return ValidationResult(true)
    }

    // Bio validation
    fun validateBio(bio: String): ValidationResult {
        if (bio.length > 500) {
            return ValidationResult(false, "Bio must be less than 500 characters")
        }
        return ValidationResult(true)
    }
}

data class ValidationResult(
    val isValid: Boolean,
    val errorMessage: String? = null
)
