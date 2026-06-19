using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RunApp.Server.Hubs;

[Authorize]
public class CallHub : Hub
{
    private readonly AppDbContext _db;
    private readonly ILogger<CallHub> _logger;
    private static readonly Dictionary<int, CallSession> ActiveCalls = new();
    private static readonly Dictionary<int, List<CallParticipant>> CallParticipants = new();

    public CallHub(AppDbContext db, ILogger<CallHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ========== INVITE SYSTEM ==========

    // POST api/calls/{callId}/invite
    public async Task InviteToCall(int callSessionId, int inviteeId)
    {
        var inviterId = GetUserId();
        if (inviterId == null) return;

        // Verify inviter is in the call
        if (!IsInCall(callSessionId, inviterId.Value))
        {
            throw new HubException("You are not in this call");
        }

        // Verify invitee is a follower
        var isFollowing = await _db.Follows
            .AnyAsync(f => f.FollowerId == inviteeId && f.FollowingId == inviterId.Value && f.IsApproved);

        if (!isFollowing)
        {
            throw new HubException("User must follow you to be invited");
        }

        // Check if invitee is already in call
        if (IsInCall(callSessionId, inviteeId))
        {
            throw new HubException("User is already in the call");
        }

        // Check existing pending invite
        var existing = await _db.CallInvites
            .FirstOrDefaultAsync(i => i.CallSessionId == callSessionId 
                                   && i.InviteeId == inviteeId 
                                   && i.Status == "pending"
                                   && i.ExpiresAt > DateTime.UtcNow);

        if (existing != null)
        {
            throw new HubException("Invite already pending");
        }

        // Create invite
        var invite = new CallInvite
        {
            CallSessionId = callSessionId,
            InviterId = inviterId.Value,
            InviteeId = inviteeId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2)
        };

        _db.CallInvites.Add(invite);
        await _db.SaveChangesAsync();

        // Get inviter info
        var inviter = await _db.Users.FindAsync(inviterId.Value);
        var callSession = ActiveCalls[callSessionId];

        // Send push notification to invitee
        await Clients.Group($"user_{inviteeId}")
            .SendAsync("IncomingCallInvite", new
            {
                inviteId = invite.Id,
                callSessionId,
                inviterId = inviterId.Value,
                inviterName = inviter?.Name,
                inviterAvatar = inviter?.Avatar,
                callType = callSession.CallType,
                participantCount = CallParticipants[callSessionId].Count,
                isGroupCall = callSession.IsGroupCall,
                groupName = callSession.GroupName,
                expiresIn = 120 // seconds
            });

        _logger.LogInformation("User {InviterId} invited {InviteeId} to call {CallId}", 
            inviterId, inviteeId, callSessionId);

        // Auto-expire after 2 minutes
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(2));
            await ExpireInvite(invite.Id);
        });
    }

    // Accept invite
    public async Task AcceptCallInvite(int inviteId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var invite = await _db.CallInvites
            .Include(i => i.CallSession)
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.InviteeId == userId);

        if (invite == null || invite.Status != "pending" || invite.ExpiresAt < DateTime.UtcNow)
        {
            throw new HubException("Invite expired or invalid");
        }

        invite.Status = "accepted";
        invite.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Add to call
        var callSession = ActiveCalls[invite.CallSessionId];
        var participant = new CallParticipant
        {
            CallSessionId = invite.CallSessionId,
            UserId = userId.Value,
            Role = "member"
        };

        if (!CallParticipants.ContainsKey(invite.CallSessionId))
            CallParticipants[invite.CallSessionId] = new List<CallParticipant>();

        CallParticipants[invite.CallSessionId].Add(participant);

        // Join SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"call_{invite.CallSessionId}");

        // Notify all participants
        var user = await _db.Users.FindAsync(userId.Value);
        await Clients.Group($"call_{invite.CallSessionId}")
            .SendAsync("ParticipantJoined", new
            {
                userId = userId.Value,
                userName = user?.Name,
                userAvatar = user?.Avatar,
                isVerified = user?.IsVerified,
                joinedAt = DateTime.UtcNow
            });

        // Send call info to new participant
        var existingParticipants = CallParticipants[invite.CallSessionId]
            .Where(p => p.UserId != userId.Value)
            .Select(p => new
            {
                p.UserId,
                p.User.Name,
                p.User.Avatar,
                p.User.IsVerified,
                p.IsMuted,
                p.IsVideoOff,
                p.IsScreenSharing
            });

        await Clients.Caller
            .SendAsync("JoinedCall", new
            {
                callSessionId = invite.CallSessionId,
                callType = callSession.CallType,
                isGroupCall = callSession.IsGroupCall,
                participants = existingParticipants,
                isHost = false
            });

        _logger.LogInformation("User {UserId} accepted invite to call {CallId}", 
            userId, invite.CallSessionId);
    }

    // Decline invite
    public async Task DeclineCallInvite(int inviteId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var invite = await _db.CallInvites.FindAsync(inviteId);
        if (invite == null || invite.InviteeId != userId) return;

        invite.Status = "declined";
        invite.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Notify inviter
        await Clients.Group($"user_{invite.InviterId}")
            .SendAsync("InviteDeclined", new
            {
                inviteId,
                inviteeId = userId.Value,
                inviteeName = (await _db.Users.FindAsync(userId.Value))?.Name
            });
    }

    // Get followers available to invite
    public async Task GetInviteableFollowers(int callSessionId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Get followers who are online and not in call
        var followers = await _db.Follows
            .Where(f => f.FollowingId == userId.Value && f.IsApproved)
            .Select(f => f.Follower)
            .Where(u => u.IsOnline && !IsInAnyCall(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Avatar,
                u.IsVerified,
                lastActive = u.LastActive
            })
            .ToListAsync();

        // Exclude already in call
        var inCallIds = CallParticipants.ContainsKey(callSessionId) 
            ? CallParticipants[callSessionId].Select(p => p.UserId).ToHashSet()
            : new HashSet<int>();

        var available = followers.Where(f => !inCallIds.Contains(f.Id));

        await Clients.Caller.SendAsync("InviteableFollowers", available);
    }

    // Bulk invite (invite multiple at once)
    public async Task BulkInvite(int callSessionId, List<int> inviteeIds)
    {
        var userId = GetUserId();
        if (userId == null) return;

        if (inviteeIds.Count > 10)
        {
            throw new HubException("Maximum 10 invites at once");
        }

        var results = new List<object>();
        foreach (var inviteeId in inviteeIds)
        {
            try
            {
                await InviteToCall(callSessionId, inviteeId);
                results.Add(new { inviteeId, success = true });
            }
            catch (Exception ex)
            {
                results.Add(new { inviteeId, success = false, error = ex.Message });
            }
        }

        await Clients.Caller.SendAsync("BulkInviteResults", results);
    }

    // ========== GROUP CALL UPGRADES ==========

    // Convert 1:1 call to group call
    public async Task UpgradeToGroupCall(int callSessionId, string groupName)
    {
        var userId = GetUserId();
        if (userId == null) return;

        if (!ActiveCalls.TryGetValue(callSessionId, out var session))
            throw new HubException("Call not found");

        if (session.CallerId != userId.Value)
            throw new HubException("Only host can upgrade");

        session.IsGroupCall = true;
        session.GroupName = groupName;

        // Update all participants
        await Clients.Group($"call_{callSessionId}")
            .SendAsync("CallUpgraded", new
            {
                groupName,
                canInvite = true
            });

        _logger.LogInformation("Call {CallId} upgraded to group: {GroupName}", 
            callSessionId, groupName);
    }

    // ========== HELPERS ==========

    private async Task ExpireInvite(int inviteId)
    {
        var invite = await _db.CallInvites.FindAsync(inviteId);
        if (invite == null || invite.Status != "pending") return;

        invite.Status = "expired";
        await _db.SaveChangesAsync();

        await Clients.Group($"user_{invite.InviteeId}")
            .SendAsync("InviteExpired", inviteId);

        await Clients.Group($"user_{invite.InviterId}")
            .SendAsync("InviteExpired", inviteId);
    }

    private bool IsInCall(int callSessionId, int userId)
    {
        return CallParticipants.ContainsKey(callSessionId) 
            && CallParticipants[callSessionId].Any(p => p.UserId == userId && p.LeftAt == null);
    }

    private bool IsInAnyCall(int userId)
    {
        return CallParticipants.Values
            .Any(participants => participants.Any(p => p.UserId == userId && p.LeftAt == null));
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}

public class CallSession
{
    public int Id { get; set; }
    public int CallerId { get; set; }
    public int? CalleeId { get; set; }
    public int ConversationId { get; set; }
    public string CallType { get; set; } = "audio";
    public bool IsGroupCall { get; set; } = false;
    public string? GroupName { get; set; }
    public string Status { get; set; } = "ringing";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}