using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RunApp.Server.Models;
using System.Collections.Concurrent;

namespace RunApp.Server.Hubs;

[Authorize]
public class CallHub : Hub
{
    private readonly AppDbContext _db;
    private readonly ILogger<CallHub> _logger;
    private static readonly ConcurrentDictionary<string, CallSession> ActiveSessions = new();
    private static readonly ConcurrentDictionary<string, List<string>> SessionConnections = new();

    public CallHub(AppDbContext db, ILogger<CallHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Caller initiates call
    public async Task InitiateCall(string callId, string calleeId, bool isVideo, string offerSdp)
    {
        var callerId = GetUserId();
        if (callerId == null) return;

        // Lyfron threat check
        var threat = await _db.LyfronThreatChecks.AddAsync(new LyfronThreatCheck
        {
            UserId = callerId.Value,
            Action = "call:initiate",
            IpAddress = Context.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Create session
        var session = new CallSession
        {
            Id = callId,
            CallerId = callerId.Value,
            CalleeId = int.Parse(calleeId),
            IsVideo = isVideo,
            Status = "ringing",
            OfferSdp = offerSdp,
            StartedAt = DateTime.UtcNow
        };

        ActiveSessions[callId] = session;
        SessionConnections[callId] = new List<string> { Context.ConnectionId };

        // Notify callee
        await Clients.User(calleeId).SendAsync("IncomingCall", 
            callId, callerId.Value, GetUserName(callerId.Value), isVideo ? "video" : "audio");

        _logger.LogInformation("Call {CallId} initiated from {Caller} to {Callee}", 
            callId, callerId, calleeId);
    }

    // Callee accepts
    public async Task AcceptCall(string callId, string answerSdp)
    {
        var calleeId = GetUserId();
        if (calleeId == null) return;

        if (!ActiveSessions.TryGetValue(callId, out var session)) return;
        if (session.CalleeId != calleeId.Value) return;

        session.Status = "connected";
        session.AnswerSdp = answerSdp;
        session.ConnectedAt = DateTime.UtcNow;

        SessionConnections[callId].Add(Context.ConnectionId);

        // Notify caller
        await Clients.User(session.CallerId.ToString())
            .SendAsync("CallAccepted", callId, answerSdp);

        _logger.LogInformation("Call {CallId} accepted by {Callee}", callId, calleeId);
    }

    // Exchange ICE candidates
    public async Task SendIceCandidate(string callId, string sdpMid, int sdpMLineIndex, string candidate)
    {
        var userId = GetUserId()?.ToString();
        if (userId == null) return;

        // Forward to other participant
        var session = ActiveSessions[callId];
        var otherUserId = session.CallerId.ToString() == userId 
            ? session.CalleeId.ToString() 
            : session.CallerId.ToString();

        await Clients.User(otherUserId).SendAsync("IceCandidate", 
            callId, sdpMid, sdpMLineIndex, candidate);
    }

    // End call
    public async Task EndCall(string callId, string reason = "user_ended")
    {
        var userId = GetUserId();
        if (userId == null) return;

        if (ActiveSessions.TryRemove(callId, out var session))
        {
            session.Status = "ended";
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = reason;

            // Save to DB for call history
            _db.CallHistory.Add(new CallHistory
            {
                CallerId = session.CallerId,
                CalleeId = session.CalleeId,
                IsVideo = session.IsVideo,
                Duration = (int)(session.EndedAt.Value - session.ConnectedAt!.Value).TotalSeconds,
                Status = reason,
                StartedAt = session.StartedAt
            });
            await _db.SaveChangesAsync();

            // Notify both parties
            await Clients.Users(session.CallerId.ToString(), session.CalleeId.ToString())
                .SendAsync("CallEnded", callId, reason);

            SessionConnections.TryRemove(callId, out _);
        }
    }

    // Reject call
    public async Task RejectCall(string callId)
    {
        if (ActiveSessions.TryRemove(callId, out var session))
        {
            await Clients.User(session.CallerId.ToString())
                .SendAsync("CallEnded", callId, "rejected");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Find any active call with this connection and end it
        foreach (var kvp in SessionConnections)
        {
            if (kvp.Value.Contains(Context.ConnectionId))
            {
                await EndCall(kvp.Key, "disconnected");
                break;
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string GetUserName(int userId)
    {
        return _db.Users.Find(userId)?.Name ?? "Unknown";
    }
}

public class CallSession
{
    public string Id { get; set; } = string.Empty;
    public int CallerId { get; set; }
    public int CalleeId { get; set; }
    public bool IsVideo { get; set; }
    public string Status { get; set; } = "ringing";
    public string OfferSdp { get; set; } = string.Empty;
    public string? AnswerSdp { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? EndReason { get; set; }
}