namespace RunApp.Server.Models;

public class Conversation
{
    public int Id { get; set; }
    public string Type { get; set; } = "direct"; // direct, group
    public string? Name { get; set; } // for groups
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public List<ConversationParticipant> Participants { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
    public ChatMessage? LastMessage { get; set; }
}

public class ConversationParticipant
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "member"; // admin, member
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
    
    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class ChatMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty; // encrypted
    public string? ContentIv { get; set; } // initialization vector for AES
    public string Type { get; set; } = "text"; // text, image, video, file, voice
    public string? MediaUrl { get; set; }
    public string? MediaThumbnail { get; set; }
    public long? MediaSize { get; set; }
    public string? ReplyToId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsForwarded { get; set; }
    
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public List<MessageReadReceipt> ReadReceipts { get; set; } = new();
}

public class MessageReadReceipt
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    
    public ChatMessage Message { get; set; } = null!;
    public User User { get; set; } = null!;
}