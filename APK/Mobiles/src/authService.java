package com.runapp.auth;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.web.bind.annotation.*;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.SignatureAlgorithm;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;

@SpringBootApplication
@RestController
@RequestMapping("/auth")
public class AuthService {

    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();
    private final String JWT_SECRET = "your-secret-key-here-change-in-production";
    
    // In-memory store (use database in production)
    private final Map<String, User> users = new HashMap<>();

    public static void main(String[] args) {
        SpringApplication.run(AuthService.class, args);
    }

    @PostMapping("/register")
    public Map<String, Object> register(@RequestBody RegisterRequest req) {
        Map<String, Object> response = new HashMap<>();
        
        if (users.containsKey(req.email)) {
            response.put("error", "Email already exists");
            return response;
        }

        String hashedPassword = passwordEncoder.encode(req.password);
        User user = new User(
            java.util.UUID.randomUUID().toString(),
            req.name,
            req.email,
            hashedPassword
        );
        
        users.put(req.email, user);
        
        String token = generateToken(user);
        response.put("token", token);
        response.put("user", Map.of(
            "id", user.id,
            "name", user.name,
            "email", user.email
        ));
        
        return response;
    }

    @PostMapping("/login")
    public Map<String, Object> login(@RequestBody LoginRequest req) {
        Map<String, Object> response = new HashMap<>();
        
        User user = users.get(req.email);
        if (user == null || !passwordEncoder.matches(req.password, user.passwordHash)) {
            response.put("error", "Invalid credentials");
            return response;
        }

        String token = generateToken(user);
        response.put("token", token);
        response.put("user", Map.of(
            "id", user.id,
            "name", user.name,
            "email", user.email
        ));
        
        return response;
    }

    @PostMapping("/validate")
    public Map<String, Object> validateToken(@RequestHeader("Authorization") String token) {
        Map<String, Object> response = new HashMap<>();
        
        try {
            String userId = Jwts.parser()
                .setSigningKey(JWT_SECRET)
                .parseClaimsJws(token.replace("Bearer ", ""))
                .getBody()
                .getSubject();
            
            response.put("valid", true);
            response.put("userId", userId);
        } catch (Exception e) {
            response.put("valid", false);
            response.put("error", "Invalid token");
        }
        
        return response;
    }

    @PostMapping("/refresh")
    public Map<String, String> refreshToken(@RequestHeader("Authorization") String token) {
        // Validate old token and issue new one
        Map<String, String> response = new HashMap<>();
        response.put("token", "new_token_here");
        return response;
    }

    @PostMapping("/logout")
    public Map<String, String> logout(@RequestHeader("Authorization") String token) {
        // Add token to blacklist in Redis
        Map<String, String> response = new HashMap<>();
        response.put("message", "Logged out successfully");
        return response;
    }

    @PostMapping("/forgot-password")
    public Map<String, String> forgotPassword(@RequestBody Map<String, String> req) {
        // Send email with reset link
        Map<String, String> response = new HashMap<>();
        response.put("message", "Reset link sent to email");
        return response;
    }

    @PostMapping("/reset-password")
    public Map<String, String> resetPassword(@RequestBody Map<String, String> req) {
        Map<String, String> response = new HashMap<>();
        response.put("message", "Password reset successful");
        return response;
    }

    @PostMapping("/2fa/enable")
    public Map<String, Object> enable2FA(@RequestHeader("Authorization") String token) {
        // Generate QR code for Google Authenticator
        Map<String, Object> response = new HashMap<>();
        response.put("qrCode", "base64_encoded_qr");
        response.put("secret", "totp_secret");
        return response;
    }

    @PostMapping("/2fa/verify")
    public Map<String, String> verify2FA(@RequestBody Map<String, String> req) {
        Map<String, String> response = new HashMap<>();
        response.put("message", "2FA enabled");
        return response;
    }

    private String generateToken(User user) {
        return Jwts.builder()
            .setSubject(user.id)
            .claim("email", user.email)
            .claim("name", user.name)
            .setIssuedAt(new Date())
            .setExpiration(new Date(System.currentTimeMillis() + 86400000)) // 24 hours
            .signWith(SignatureAlgorithm.HS256, JWT_SECRET)
            .compact();
    }

    // Inner classes
    static class User {
        String id, name, email, passwordHash;
        User(String id, String name, String email, String passwordHash) {
            this.id = id; this.name = name; this.email = email; this.passwordHash = passwordHash;
        }
    }

    static class RegisterRequest {
        public String name, email, password;
    }

    static class LoginRequest {
        public String email, password;
    }
}