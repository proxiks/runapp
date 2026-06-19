using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RunApp.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(AppDbContext db, ILogger<ChatHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Join user's personal group for direct messages
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        
        // Join all conversation groups
        var conversations = await _db.ConversationParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.ConversationId)
            .ToListAsync();

        foreach (var convId in conversations)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{convId}");
        }

        // Update online status
        await Clients.Others.SendAsync("UserOnline", userId);

        _logger.LogInformation("User {UserId} connected to chat", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            await Clients.Others.SendAsync("UserOffline", userId);
            _logger.LogInformation("User {UserId} disconnected", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Send message
    public async Task SendMessage(SendMessageRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Verify user is in conversation
        var isParticipant = await _db.ConversationParticipants
            .AnyAsync(p => p.ConversationId == req.ConversationId && p.UserId == userId);

        if (!isParticipant)
        {
            throw new HubException("Not a participant in this conversation");
        }

        // Check verified-only restriction
        var conv = await _db.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == req.ConversationId);

        if (conv?.Type == "verified_only")
        {
            var sender = await _db.Users.FindAsync(userId);
            if (sender?.IsVerified != true)
            {
                throw new HubException("This conversation requires verified status");
            }
        }

        // Save message
        var message = new ChatMessage
        {
            ConversationId = req.ConversationId,
            SenderId = userId.Value,
            Content = req.Content, // Already encrypted by client
            ContentIv = req.ContentIv,
            Type = req.Type,
            MediaUrl = req.MediaUrl,
            ReplyToId = req.ReplyToId
        };

        _db.ChatMessages.Add(message);
        
        // Update conversation timestamp
        conv!.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync();

        // Get sender info
        var senderUser = await _db.Users.FindAsync(userId);

        // Broadcast to conversation
        await Clients.Group($"conv_{req.ConversationId}")
            .SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                conversationId = message.ConversationId,
                senderId = message.SenderId,
                senderName = senderUser?.Name,
                senderAvatar = senderUser?.Avatar,
                content = message.Content,
                type = message.Type,
                mediaUrl = message.MediaUrl,
                replyToId = message.ReplyToId,
                createdAt = message.CreatedAt,
                isVerified = senderUser?.IsVerified
            });

        // Send notification to offline participants
        var onlineParticipants = await GetOnlineParticipants(req.ConversationId);
        var offlineParticipants = conv.Participants
            .Where(p => p.UserId != userId && !onlineParticipants.Contains(p.UserId))
            .Select(p => p.UserId);

        foreach (var offlineUserId in offlineParticipants)
        {
            await Clients.Group($"user_{offlineUserId}")
                .SendAsync("NewMessageNotification", new
                {
                    conversationId = req.ConversationId,
                    senderName = senderUser?.Name,
                    preview = req.Type == "text" ? DecryptPreview(req.Content, req.ContentIv) : "Media",
                    timestamp = message.CreatedAt
                });
        }

        _logger.LogInformation("Message sent in conv {ConvId} by user {UserId}", 
            req.ConversationId, userId);
    }

    // Typing indicator
    public async Task Typing(int conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        await Clients.Group($"conv_{conversationId}")
            .SendAsync("UserTyping", new
            {
                userId,
                conversationId
            });
    }

    // Mark as read
    public async Task MarkRead(int conversationId, int lastMessageId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var messages = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.Id <= lastMessageId)
            .Where(m => !m.ReadReceipts.Any(r => r.UserId == userId))
            .ToListAsync();

        foreach (var msg in messages)
        {
            _db.MessageReadReceipts.Add(new MessageReadReceipt
            {
                MessageId = msg.Id,
                UserId = userId.Value
            });
        }

        await _db.SaveChangesAsync();

        // Notify sender
        await Clients.Group($"conv_{conversationId}")
            .SendAsync("MessagesRead", new
            {
                userId,
                conversationId,
                lastMessageId
            });
    }

    // Join new conversation (when created)
    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
    }

    // Create direct conversation
    public async Task CreateDirectConversation(int otherUserId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Check if conversation already exists
        var existing = await _db.Conversations
            .Where(c => c.Type == "direct")
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Where(c => c.Participants.Any(p => p.UserId == otherUserId))
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            await Clients.Caller.SendAsync("ConversationCreated", existing.Id);
            return;
        }

        var conversation = new Conversation
        {
            Type = "direct",
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = userId.Value },
                new() { UserId = otherUserId }
            }
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        // Join both users
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversation.Id}");
        await Clients.Group($"user_{otherUserId}")
            .SendAsync("NewConversation", new
            {
                conversationId = conversation.Id,
                userId = userId.Value
            });

        await Clients.Caller.SendAsync("ConversationCreated", conversation.Id);
    }

    // Create verified-only group
    public async Task CreateVerifiedGroup(string name, List<int> memberIds)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var creator = await _db.Users.FindAsync(userId);
        if (creator?.IsVerified != true)
        {
            throw new HubException("Only verified users can create verified groups");
        }

        var conversation = new Conversation
        {
            Type = "verified_only",
            Name = name,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = userId.Value, Role = "admin" }
            }
        };

        foreach (var memberId in memberIds)
        {
            var member = await _db.Users.FindAsync(memberId);
            if (member?.IsVerified == true)
            {
                conversation.Participants.Add(new ConversationParticipant
                {
                    UserId = memberId,
                    Role = "member"
                });
            }
        }

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        // Notify all members
        foreach (var participant in conversation.Participants)
        {
            await Clients.Group($"user_{participant.UserId}")
                .SendAsync("AddedToGroup", new
                {
                    conversationId = conversation.Id,
                    name = conversation.Name,
                    addedBy = userId
                });
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private async Task<List<int>> GetOnlineParticipants(int conversationId)
    {
        // This is simplified - in production use a presence service (Redis)
        return new List<int>();
    }

    private string DecryptPreview(string encryptedContent, string? iv)
    {
        // Server never decrypts - this is just for notification preview
        // In production, send encrypted and let client decrypt
        return "New message";
    }
}

public class SendMessageRequest
{
    public int ConversationId { get; set; }
    public string Content { get; set; } = string.Empty; // Base64 encrypted
    public string? ContentIv { get; set; }
    public string Type { get; set; } = "text";
    public string? MediaUrl { get; set; }
    public string? ReplyToId { get; set; }
}