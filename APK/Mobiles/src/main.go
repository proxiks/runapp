package main

import (
	"context"
	"encoding/json"
	"log"
	"net/http"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/gorilla/websocket"
	"github.com/jackc/pgx/v5/pgxpool"
	"github.com/redis/go-redis/v9"
)

// Database
var db *pgxpool.Pool
var redisClient *redis.Client

// WebSocket upgrader
var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

// Models
type User struct {
	ID              string    `json:"id"`
	Name            string    `json:"name"`
	Email           string    `json:"email"`
	Password        string    `json:"-"`
	Bio             string    `json:"bio"`
	AvatarURL       string    `json:"avatar_url"`
	FollowersCount  int       `json:"followers_count"`
	FollowingCount  int       `json:"following_count"`
	FriendsCount    int       `json:"friends_count"`
	CreatedAt       time.Time `json:"created_at"`
}

type Post struct {
	ID        string    `json:"id"`
	AuthorID  string    `json:"author_id"`
	Content   string    `json:"content"`
	ImageURL  string    `json:"image_url"`
	VideoURL  string    `json:"video_url"`
	Likes     int       `json:"likes"`
	Comments  int       `json:"comments"`
	Shares    int       `json:"shares"`
	CreatedAt time.Time `json:"created_at"`
}

type Message struct {
	ID         string    `json:"id"`
	SenderID   string    `json:"sender_id"`
	ReceiverID string    `json:"receiver_id"`
	Content    string    `json:"content"`
	CreatedAt  time.Time `json:"created_at"`
	IsRead     bool      `json:"is_read"`
}

type Notification struct {
	ID        string    `json:"id"`
	UserID    string    `json:"user_id"`
	Type      string    `json:"type"`
	Content   string    `json:"content"`
	IsRead    bool      `json:"is_read"`
	CreatedAt time.Time `json:"created_at"`
}

// WebSocket clients
var clients = make(map[string]*websocket.Conn)

func main() {
	// Connect to PostgreSQL
	var err error
	db, err = pgxpool.New(context.Background(), "postgres://user:pass@localhost/jatinbook")
	if err != nil {
		log.Fatal(err)
	}
	defer db.Close()

	// Connect to Redis
	redisClient = redis.NewClient(&redis.Options{
		Addr: "localhost:6379",
	})
	defer redisClient.Close()

	// Setup Gin
	r := gin.Default()

	// Auth routes
	r.POST("/api/auth/register", register)
	r.POST("/api/auth/login", login)

	// Protected routes
	api := r.Group("/api")
	api.Use(authMiddleware())
	{
		// Users
		api.GET("/users/:id", getUser)
		api.GET("/users/search", searchUsers)
		api.POST("/users/:id/follow", followUser)
		api.POST("/users/:id/unfollow", unfollowUser)
		api.POST("/users/:id/friend-request", sendFriendRequest)
		api.POST("/friend-requests/:id/accept", acceptFriendRequest)
		api.POST("/friend-requests/:id/reject", rejectFriendRequest)

		// Posts
		api.GET("/feed", getFeed)
		api.POST("/posts", createPost)
		api.GET("/posts/:id", getPost)
		api.POST("/posts/:id/like", likePost)
		api.POST("/posts/:id/comment", addComment)
		api.DELETE("/posts/:id", deletePost)

		// Comments
		api.GET("/posts/:id/comments", getComments)

		// Messages
		api.GET("/messages/:userId", getMessages)
		api.POST("/messages", sendMessage)

		// Notifications
		api.GET("/notifications", getNotifications)
		api.POST("/notifications/:id/read", markNotificationRead)

		// Upload
		api.POST("/upload", uploadFile)

		// WebSocket
		api.GET("/ws", handleWebSocket)
	}

	r.Run(":8080")
}

// Auth handlers
func register(c *gin.Context) {
	var req struct {
		Name     string `json:"name"`
		Email    string `json:"email"`
		Password string `json:"password"`
	}
	if err := c.BindJSON(&req); err != nil {
		c.JSON(400, gin.H{"error": "invalid request"})
		return
	}

	// Hash password (simplified)
	// In production use bcrypt
	hashedPassword := hashPassword(req.Password)

	var userID string
	err := db.QueryRow(context.Background(),
		`INSERT INTO users (name, email, password) VALUES ($1, $2, $3) RETURNING id`,
		req.Name, req.Email, hashedPassword).Scan(&userID)

	if err != nil {
		c.JSON(500, gin.H{"error": "registration failed"})
		return
	}

	c.JSON(201, gin.H{"id": userID, "message": "registered"})
}

func login(c *gin.Context) {
	var req struct {
		Email    string `json:"email"`
		Password string `json:"password"`
	}
	if err := c.BindJSON(&req); err != nil {
		c.JSON(400, gin.H{"error": "invalid request"})
		return
	}

	var user User
	var hashedPassword string
	err := db.QueryRow(context.Background(),
		`SELECT id, name, email, password FROM users WHERE email = $1`,
		req.Email).Scan(&user.ID, &user.Name, &user.Email, &hashedPassword)

	if err != nil || !checkPassword(req.Password, hashedPassword) {
		c.JSON(401, gin.H{"error": "invalid credentials"})
		return
	}

	token := generateJWT(user.ID)
	c.JSON(200, gin.H{"token": token, "user": user})
}

// Post handlers
func getFeed(c *gin.Context) {
	userID := c.GetString("userID")
	rows, err := db.Query(context.Background(),
		`SELECT p.id, p.author_id, p.content, p.image_url, p.video_url, p.likes, p.comments, p.shares, p.created_at, u.name
		FROM posts p
		JOIN users u ON p.author_id = u.id
		WHERE p.author_id = $1 OR p.author_id IN (
			SELECT following_id FROM follows WHERE follower_id = $1
		)
		ORDER BY p.created_at DESC LIMIT 50`, userID)
	if err != nil {
		c.JSON(500, gin.H{"error": "failed to get feed"})
		return
	}
	defer rows.Close()

	var posts []Post
	for rows.Next() {
		var p Post
		var authorName string
		rows.Scan(&p.ID, &p.AuthorID, &p.Content, &p.ImageURL, &p.VideoURL, &p.Likes, &p.Comments, &p.Shares, &p.CreatedAt, &authorName)
		posts = append(posts, p)
	}

	c.JSON(200, posts)
}

func createPost(c *gin.Context) {
	userID := c.GetString("userID")
	var req struct {
		Content  string `json:"content"`
		ImageURL string `json:"image_url"`
		VideoURL string `json:"video_url"`
	}
	if err := c.BindJSON(&req); err != nil {
		c.JSON(400, gin.H{"error": "invalid request"})
		return
	}

	var postID string
	err := db.QueryRow(context.Background(),
		`INSERT INTO posts (author_id, content, image_url, video_url) VALUES ($1, $2, $3, $4) RETURNING id`,
		userID, req.Content, req.ImageURL, req.VideoURL).Scan(&postID)

	if err != nil {
		c.JSON(500, gin.H{"error": "failed to create post"})
		return
	}

	// Notify followers
	go notifyFollowers(userID, "new_post", "New post from someone you follow")

	c.JSON(201, gin.H{"id": postID})
}

// Message handlers
func getMessages(c *gin.Context) {
	userID := c.GetString("userID")
	otherUserID := c.Param("userID")

	rows, err := db.Query(context.Background(),
		`SELECT id, sender_id, receiver_id, content, created_at, is_read
		FROM messages
		WHERE (sender_id = $1 AND receiver_id = $2) OR (sender_id = $2 AND receiver_id = $1)
		ORDER BY created_at DESC LIMIT 100`,
		userID, otherUserID)
	if err != nil {
		c.JSON(500, gin.H{"error": "failed to get messages"})
		return
	}
	defer rows.Close()

	var messages []Message
	for rows.Next() {
		var m Message
		rows.Scan(&m.ID, &m.SenderID, &m.ReceiverID, &m.Content, &m.CreatedAt, &m.IsRead)
		messages = append(messages, m)
	}

	c.JSON(200, messages)
}

func sendMessage(c *gin.Context) {
	userID := c.GetString("userID")
	var req struct {
		ReceiverID string `json:"receiver_id"`
		Content    string `json:"content"`
	}
	if err := c.BindJSON(&req); err != nil {
		c.JSON(400, gin.H{"error": "invalid request"})
		return
	}

	var msgID string
	err := db.QueryRow(context.Background(),
		`INSERT INTO messages (sender_id, receiver_id, content) VALUES ($1, $2, $3) RETURNING id`,
		userID, req.ReceiverID, req.Content).Scan(&msgID)

	if err != nil {
		c.JSON(500, gin.H{"error": "failed to send message"})
		return
	}

	// Send via WebSocket if online
	if conn, ok := clients[req.ReceiverID]; ok {
		conn.WriteJSON(gin.H{
			"type": "message",
			"data": gin.H{
				"sender_id": userID,
				"content":   req.Content,
			},
		})
	}

	// Store in Redis for offline users
	redisClient.LPush(context.Background(), "messages:"+req.ReceiverID, msgID)

	c.JSON(201, gin.H{"id": msgID})
}

// WebSocket handler
func handleWebSocket(c *gin.Context) {
	userID := c.GetString("userID")
	conn, err := upgrader.Upgrade(c.Writer, c.Request, nil)
	if err != nil {
		return
	}
	defer conn.Close()

	clients[userID] = conn

	// Send offline messages from Redis
	msgs, _ := redisClient.LRange(context.Background(), "messages:"+userID, 0, -1).Result()
	for _, msgID := range msgs {
		conn.WriteJSON(gin.H{"type": "offline_message", "id": msgID})
	}
	redisClient.Del(context.Background(), "messages:"+userID)

	for {
		var msg map[string]interface{}
		if err := conn.ReadJSON(&msg); err != nil {
			delete(clients, userID)
			break
		}
		// Handle typing, read receipts, etc.
	}
}

// Helper functions (simplified)
func hashPassword(password string) string {
	// Use bcrypt in production
	return password
}

func checkPassword(password, hash string) bool {
	return password == hash
}

func generateJWT(userID string) string {
	// Use proper JWT library in production
	return "token_" + userID
}

func authMiddleware() gin.HandlerFunc {
	return func(c *gin.Context) {
		token := c.GetHeader("Authorization")
		if token == "" {
			c.AbortWithStatusJSON(401, gin.H{"error": "unauthorized"})
			return
		}
		// Validate JWT and set userID
		c.Set("userID", "user_1")
		c.Next()
	}
}

func getUser(c *gin.Context)         { c.JSON(200, gin.H{}) }
func searchUsers(c *gin.Context)     { c.JSON(200, gin.H{}) }
func followUser(c *gin.Context)      { c.JSON(200, gin.H{}) }
func unfollowUser(c *gin.Context)    { c.JSON(200, gin.H{}) }
func sendFriendRequest(c *gin.Context) { c.JSON(200, gin.H{}) }
func acceptFriendRequest(c *gin.Context) { c.JSON(200, gin.H{}) }
func rejectFriendRequest(c *gin.Context) { c.JSON(200, gin.H{}) }
func getPost(c *gin.Context)         { c.JSON(200, gin.H{}) }
func likePost(c *gin.Context)        { c.JSON(200, gin.H{}) }
func addComment(c *gin.Context)      { c.JSON(200, gin.H{}) }
func getComments(c *gin.Context)     { c.JSON(200, gin.H{}) }
func deletePost(c *gin.Context)     { c.JSON(200, gin.H{}) }
func getNotifications(c *gin.Context) { c.JSON(200, gin.H{}) }
func markNotificationRead(c *gin.Context) { c.JSON(200, gin.H{}) }
func uploadFile(c *gin.Context)      { c.JSON(200, gin.H{}) }
func notifyFollowers(userID, notifType, content string) {}