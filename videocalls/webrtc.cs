using System.Runtime.InteropServices;

namespace RunApp.Desktop.Services;

public class WebRtcService : IDisposable
{
    private const string DllName = "lyfron_webrtc.dll";

    // P/Invoke declarations
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr lyfron_create_peer_connection(
        [MarshalAs(UnmanagedType.LPStr)] string stun_server,
        IceCandidateCallback ice_cb,
        ConnectionStateCallback state_cb);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void lyfron_destroy_peer_connection(IntPtr pc);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lyfron_add_audio_track(IntPtr pc, [MarshalAs(UnmanagedType.LPStr)] string track_id);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lyfron_add_video_track(IntPtr pc, [MarshalAs(UnmanagedType.LPStr)] string track_id, IntPtr native_window);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lyfron_create_offer(IntPtr pc, IntPtr sdp_out, UIntPtr sdp_len);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lyfron_create_answer(IntPtr pc, IntPtr sdp_out, UIntPtr sdp_len);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lyfron_set_remote_description(IntPtr pc, [MarshalAs(UnmanagedType.LPStr)] string type, [MarshalAs(UnmanagedType.LPStr)] string sdp);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lyfron_set_local_description(IntPtr pc, [MarshalAs(UnmanagedType.LPStr)] string type, [MarshalAs(UnmanagedType.LPStr)] string sdp);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int lyfron_add_ice_candidate(IntPtr pc, [MarshalAs(UnmanagedType.LPStr)] string sdp_mid, int mline_index, [MarshalAs(UnmanagedType.LPStr)] string candidate);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void lyfron_set_audio_enabled(IntPtr pc, int enabled);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void lyfron_set_video_enabled(IntPtr pc, int enabled);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void lyfron_set_speaker_muted(IntPtr pc, int muted);

    // Delegates
    private delegate void IceCandidateCallback([MarshalAs(UnmanagedType.LPStr)] string sdp_mid, int sdp_mline_index, [MarshalAs(UnmanagedType.LPStr)] string candidate);
    private delegate void ConnectionStateCallback(int state);

    public event EventHandler<IceCandidate>? OnIceCandidate;
    public event EventHandler<ConnectionState>? OnConnectionStateChanged;

    private IntPtr _peerConnection;
    private IceCandidateCallback? _iceCallback;
    private ConnectionStateCallback? _stateCallback;
    private bool _disposed;

    public bool IsAudioEnabled { get; private set; } = true;
    public bool IsVideoEnabled { get; private set; } = true;
    public bool IsSpeakerMuted { get; private set; } = false;

    public void Initialize(string stunServer = "stun:stun.l.google.com:19302")
    {
        _iceCallback = (sdpMid, mlineIndex, candidate) =>
        {
            OnIceCandidate?.Invoke(this, new IceCandidate
            {
                SdpMid = sdpMid,
                SdpMlineIndex = mlineIndex,
                Candidate = candidate
            });
        };

        _stateCallback = (state) =>
        {
            OnConnectionStateChanged?.Invoke(this, (ConnectionState)state);
        };

        _peerConnection = lyfron_create_peer_connection(stunServer, _iceCallback, _stateCallback);
        
        if (_peerConnection == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create peer connection");
    }

    public void AddAudioTrack(string trackId = "audio")
    {
        if (_peerConnection == IntPtr.Zero) return;
        lyfron_add_audio_track(_peerConnection, trackId);
    }

    public void AddVideoTrack(IntPtr nativeWindow, string trackId = "video")
    {
        if (_peerConnection == IntPtr.Zero) return;
        lyfron_add_video_track(_peerConnection, trackId, nativeWindow);
    }

    public async Task<string> CreateOfferAsync()
    {
        if (_peerConnection == IntPtr.Zero) throw new InvalidOperationException("Not initialized");

        var buffer = Marshal.AllocHGlobal(65536);
        try
        {
            var result = lyfron_create_offer(_peerConnection, buffer, (UIntPtr)65536);
            if (result != 0) throw new InvalidOperationException("Failed to create offer");

            return Marshal.PtrToStringAnsi(buffer)!;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public async Task<string> CreateAnswerAsync()
    {
        if (_peerConnection == IntPtr.Zero) throw new InvalidOperationException("Not initialized");

        var buffer = Marshal.AllocHGlobal(65536);
        try
        {
            var result = lyfron_create_answer(_peerConnection, buffer, (UIntPtr)65536);
            if (result != 0) throw new InvalidOperationException("Failed to create answer");

            return Marshal.PtrToStringAnsi(buffer)!;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void SetRemoteDescription(string type, string sdp)
    {
        if (_peerConnection == IntPtr.Zero) return;
        lyfron_set_remote_description(_peerConnection, type, sdp);
    }

    public void SetLocalDescription(string type, string sdp)
    {
        if (_peerConnection == IntPtr.Zero) return;
        lyfron_set_local_description(_peerConnection, type, sdp);
    }

    public void AddIceCandidate(IceCandidate candidate)
    {
        if (_peerConnection == IntPtr.Zero) return;
        lyfron_add_ice_candidate(_peerConnection, candidate.SdpMid, candidate.SdpMlineIndex, candidate.Candidate);
    }

    public void ToggleAudio()
    {
        IsAudioEnabled = !IsAudioEnabled;
        lyfron_set_audio_enabled(_peerConnection, IsAudioEnabled ? 1 : 0);
    }

    public void ToggleVideo()
    {
        IsVideoEnabled = !IsVideoEnabled;
        lyfron_set_video_enabled(_peerConnection, IsVideoEnabled ? 1 : 0);
    }

    public void ToggleSpeaker()
    {
        IsSpeakerMuted = !IsSpeakerMuted;
        lyfron_set_speaker_muted(_peerConnection, IsSpeakerMuted ? 1 : 0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_peerConnection != IntPtr.Zero)
        {
            lyfron_destroy_peer_connection(_peerConnection);
            _peerConnection = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }
}

public class IceCandidate
{
    public string SdpMid { get; set; } = string.Empty;
    public int SdpMlineIndex { get; set; }
    public string Candidate { get; set; } = string.Empty;
}

public enum ConnectionState
{
    New = 0,
    Connecting = 1,
    Connected = 2,
    Disconnected = 3,
    Failed = 4,
    Closed = 5
}