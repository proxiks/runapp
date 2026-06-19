using Microsoft.AspNetCore.SignalR.Client;

namespace RunApp.Desktop.Services;

public class CallManager : IDisposable
{
    private readonly WebRtcService _webrtc;
    private readonly HubConnection _signaling;
    private readonly ILogger<CallManager> _logger;

    public event EventHandler<CallState>? OnCallStateChanged;
    public event EventHandler<IncomingCallData>? OnIncomingCall;
    public event EventHandler<RemoteVideoFrame>? OnRemoteVideoFrame;
    public event EventHandler<string>? OnCallError;

    public CallState CurrentState { get; private set; } = CallState.Idle;
    public CallType ActiveCallType { get; private set; }
    public int? CurrentConversationId { get; private set; }

    public CallManager(string authToken, ILogger<CallManager> logger)
    {
        _logger = logger;
        _webrtc = new WebRtcService();
        
        _signaling = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/hubs/call", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(authToken)!;
            })
            .WithAutomaticReconnect()
            .Build();

        SetupSignalingHandlers();
        SetupWebRtcHandlers();
    }

    private void SetupSignalingHandlers()
    {
        // Incoming call
        _signaling.On<IncomingCallData>("IncomingCall", data =>
        {
            _logger.LogInformation("Incoming {Type} call from {Caller}", data.CallType, data.CallerName);
            OnIncomingCall?.Invoke(this, data);
        });

        // Call accepted
        _signaling.On<string>("CallAccepted", async sdp =>
        {
            _webrtc.SetRemoteDescription("answer", sdp);
            await _signaling.InvokeAsync("SendIceCandidates", await GatherIceCandidates());
            SetState(CallState.Connected);
        });

        // Call rejected
        _signaling.On("CallRejected", () =>
        {
            SetState(CallState.Idle);
            Cleanup();
        });

        // Call ended
        _signaling.On("CallEnded", () =>
        {
            SetState(CallState.Idle);
            Cleanup();
        });

        // Receive ICE candidates
        _signaling.On<List<IceCandidate>>("IceCandidates", candidates =>
        {
            foreach (var candidate in candidates)
            {
                _webrtc.AddIceCandidate(candidate);
            }
        });

        // Receive offer (for callee)
        _signaling.On<string>("ReceiveOffer", async sdp =>
        {
            _webrtc.SetRemoteDescription("offer", sdp);
            var answer = await _webrtc.CreateAnswerAsync();
            _webrtc.SetLocalDescription("answer", answer);
            await _signaling.InvokeAsync("SendAnswer", answer);
        });
    }

    private void SetupWebRtcHandlers()
    {
        _webrtc.OnIceCandidate += (s, candidate) =>
        {
            _ = _signaling.InvokeAsync("SendIceCandidate", candidate);
        };

        _webrtc.OnConnectionStateChanged += (s, state) =>
        {
            switch (state)
            {
                case ConnectionState.Connected:
                    SetState(CallState.Connected);
                    break;
                case ConnectionState.Disconnected:
                case ConnectionState.Failed:
                case ConnectionState.Closed:
                    SetState(CallState.Ended);
                    Cleanup();
                    break;
            }
        };
    }

    public async Task StartCall(int conversationId, CallType type)
    {
        if (CurrentState != CallState.Idle)
        {
            OnCallError?.Invoke(this, "Already in a call");
            return;
        }

        try
        {
            SetState(CallState.Initiating);
            ActiveCallType = type;
            CurrentConversationId = conversationId;

            // Initialize WebRTC
            _webrtc.Initialize();
            _webrtc.AddAudioTrack();

            if (type == CallType.Video)
            {
                // Get native window handle for video preview
                var windowHandle = GetPreviewWindowHandle();
                _webrtc.AddVideoTrack(windowHandle);
            }

            // Create and send offer
            var offer = await _webrtc.CreateOfferAsync();
            _webrtc.SetLocalDescription("offer", offer);

            await _signaling.InvokeAsync("InitiateCall", new
            {
                conversationId,
                callType = type.ToString().ToLower(),
                offer
            });

            SetState(CallState.Ringing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start call");
            SetState(CallState.Idle);
            OnCallError?.Invoke(this, ex.Message);
        }
    }

    public async Task AcceptCall(int conversationId)
    {
        if (CurrentState != CallState.Idle)
        {
            OnCallError?.Invoke(this, "Cannot accept - not idle");
            return;
        }

        try
        {
            SetState(CallState.Connecting);
            CurrentConversationId = conversationId;

            _webrtc.Initialize();
            _webrtc.AddAudioTrack();

            // Wait for offer via signaling, then create answer
            await _signaling.InvokeAsync("AcceptCall", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept call");
            SetState(CallState.Idle);
        }
    }

    public async Task RejectCall(int conversationId)
    {
        await _signaling.InvokeAsync("RejectCall", conversationId);
        SetState(CallState.Idle);
    }

    public async Task EndCall()
    {
        if (CurrentState == CallState.Idle) return;

        await _signaling.InvokeAsync("EndCall", CurrentConversationId);
        SetState(CallState.Ended);
        Cleanup();
    }

    public void ToggleAudio() => _webrtc.ToggleAudio();
    public void ToggleVideo() => _webrtc.ToggleVideo();
    public void ToggleSpeaker() => _webrtc.ToggleSpeaker();

    private void SetState(CallState state)
    {
        CurrentState = state;
        OnCallStateChanged?.Invoke(this, state);
    }

    private void Cleanup()
    {
        _webrtc.Dispose();
        CurrentConversationId = null;
    }

    private IntPtr GetPreviewWindowHandle()
    {
        // Return WPF window handle for video preview
        return IntPtr.Zero; // Implement based on your UI
    }

    private async Task<List<IceCandidate>> GatherIceCandidates()
    {
        // Gather ICE candidates after creating offer/answer
        await Task.Delay(2000); // Wait for gathering
        return new List<IceCandidate>();
    }

    private async Task ConnectSignaling()
    {
        if (_signaling.State == HubConnectionState.Disconnected)
        {
            await _signaling.StartAsync();
        }
    }

    public void Dispose()
    {
        _webrtc.Dispose();
        _signaling.DisposeAsync().GetAwaiter().GetResult();
    }
}

public enum CallState
{
    Idle,
    Initiating,
    Ringing,
    Connecting,
    Connected,
    Ended
}

public enum CallType
{
    Audio,
    Video
}

public class IncomingCallData
{
    public int ConversationId { get; set; }
    public int CallerId { get; set; }
    public string CallerName { get; set; } = string.Empty;
    public string CallerAvatar { get; set; } = string.Empty;
    public CallType CallType { get; set; }
    public DateTime StartedAt { get; set; }
}

public class RemoteVideoFrame
{
    public IntPtr FrameData { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}