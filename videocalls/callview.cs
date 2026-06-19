using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RunApp.Desktop.Views;

public partial class CallView : UserControl
{
    private readonly CallManager _callManager;
    private DispatcherTimer? _durationTimer;
    private DateTime _callStartTime;

    public CallView(CallManager callManager)
    {
        InitializeComponent();
        _callManager = callManager;

        _callManager.OnCallStateChanged += OnCallStateChanged;
        _callManager.OnIncomingCall += OnIncomingCall;
    }

    private void OnCallStateChanged(object? sender, CallState state)
    {
        Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case CallState.Connected:
                    CallStatusText.Text = "Connected";
                    StartDurationTimer();
                    break;
                case CallState.Ended:
                    StopDurationTimer();
                    // Navigate back
                    break;
            }
        });
    }

    private void OnIncomingCall(object? sender, IncomingCallData data)
    {
        Dispatcher.Invoke(() =>
        {
            IncomingOverlay.Visibility = Visibility.Visible;
            CallerInitial.Text = data.CallerName[0].ToString();
            CallerNameText.Text = data.CallerName;
            IncomingCallType.Text = data.CallType == CallType.Video 
                ? "Incoming video call" 
                : "Incoming audio call";
        });
    }

    private void StartDurationTimer()
    {
        _callStartTime = DateTime.Now;
        _durationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _durationTimer.Tick += (s, e) =>
        {
            var duration = DateTime.Now - _callStartTime;
            CallDurationText.Text = $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        };
        _durationTimer.Start();
    }

    private void StopDurationTimer()
    {
        _durationTimer?.Stop();
    }

    private void OnToggleAudio(object sender, RoutedEventArgs e)
    {
        _callManager.ToggleAudio();
        AudioToggle.Opacity = _callManager.IsAudioEnabled ? 1 : 0.5;
    }

    private void OnToggleVideo(object sender, RoutedEventArgs e)
    {
        _callManager.ToggleVideo();
        VideoToggle.Opacity = _callManager.IsVideoEnabled ? 1 : 0.5;
    }

    private void OnToggleSpeaker(object sender, RoutedEventArgs e)
    {
        _callManager.ToggleSpeaker();
    }

    private void OnFlipCamera(object sender, RoutedEventArgs e)
    {
        // Switch front/back camera
    }

    private async void OnEndCall(object sender, RoutedEventArgs e)
    {
        await _callManager.EndCall();
    }

    private async void OnAcceptCall(object sender, RoutedEventArgs e)
    {
        IncomingOverlay.Visibility = Visibility.Collapsed;
        // Get conversation ID from incoming data
        await _callManager.AcceptCall(0); // Pass actual ID
    }

    private async void OnDeclineCall(object sender, RoutedEventArgs e)
    {
        IncomingOverlay.Visibility = Visibility.Collapsed;
        await _callManager.RejectCall(0); // Pass actual ID
    }
}