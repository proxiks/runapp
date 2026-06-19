namespace RunApp.Server.Models;

public class CallInvite
{
    public int Id { get; set; }
    public int CallSessionId { get; set; }
    public int InviterId { get; set; }
    public int InviteeId { get; set; }
    public string Status { get; set; } = "pending"; // pending, accepted, declined, expired
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(2);
    public DateTime? RespondedAt { get; set; }

    public CallSession CallSession { get; set; } = null!;
    public User Inviter { get; set; } = null!;
    public User Invitee { get; set; } = null!;
}

public class CallParticipant
{
    public int Id { get; set; }
    public int CallSessionId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "member"; // host, co_host, member
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsMuted { get; set; }
    public bool IsVideoOff { get; set; }
    public bool IsScreenSharing { get; set; }

    public CallSession CallSession { get; set; } = null!;
    public User User { get; set; } = null!;
}