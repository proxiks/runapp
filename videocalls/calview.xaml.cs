using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Net.Http.Json;

namespace RunApp.Desktop.Views;

public partial class CallView : UserControl
{
    private readonly CallManager _callManager;
    private readonly HttpClient _http;
    private List<FollowerViewModel> _availableFollowers = new();
    private HashSet<int> _selectedInvitees = new();

    // Add to existing constructor
    public CallView(CallManager callManager)
    {
        InitializeComponent();
        _callManager = callManager;
        _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/api/"),
            DefaultRequestHeaders =
            {
                { "Authorization", $"Bearer {Properties.Settings.Default.AuthToken}" }
            }
        };

        _callManager.OnCallStateChanged += OnCallStateChanged;
        _callManager.OnParticipantJoined += OnParticipantJoined;
        _callManager.OnParticipantLeft += OnParticipantLeft;
        _callManager.OnIncomingInvite += OnIncomingInvite;
    }

    // ========== INVITE BUTTON ==========

    private void OnInviteClick(object sender, RoutedEventArgs e)
    {
        // Show participants panel with invite option
        ShowParticipantsPanel();
        LoadInviteableFollowers();
    }

    private void ShowParticipantsPanel()
    {
        var storyboard = new Storyboard();
        var animation = new ThicknessAnimation
        {
            From = new Thickness(0, 0, -280, 0),
            To = new Thickness(0),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animation, ParticipantsPanel);
        Storyboard.SetTargetProperty(animation, new PropertyPath(MarginProperty));
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void HideParticipantsPanel()
    {
        var storyboard = new Storyboard();
        var animation = new ThicknessAnimation
        {
            From = new Thickness(0),
            To = new Thickness(0, 0, -280, 0),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(animation, ParticipantsPanel);
        Storyboard.SetTargetProperty(animation, new PropertyPath(MarginProperty));
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    // ========== LOAD FOLLOWERS ==========

    private async void LoadInviteableFollowers()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<FollowerDto>>(
                $"calls/{_callManager.CurrentCallId}/inviteable-followers");

            if (response == null) return;

            _availableFollowers = response.Select(f => new FollowerViewModel
            {
                Id = f.Id,
                Name = f.Name,
                Initial = f.Name[0].ToString(),
                Status = f.IsOnline ? "Online" : $"Active {FormatTime(f.LastActive)}",
                IsOnline = f.IsOnline,
                IsVerified = f.IsVerified,
                VerifiedBadge = f.IsVerified ? new VerifiedBadge() : null,
                IsSelected = false
            }).ToList();

            InviteFollowersList.ItemsSource = _availableFollowers;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load followers: {ex.Message}");
        }
    }

    private void OnInviteSearch(object sender, TextChangedEventArgs e)
    {
        var query = InviteSearch.Text.ToLower();
        var filtered = _availableFollowers.Where(f => 
            f.Name.ToLower().Contains(query)).ToList();
        InviteFollowersList.ItemsSource = filtered;
    }

    // ========== SEND INVITE ==========

    private async void OnSendInvite(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.CommandParameter is not int followerId) return;

        try
        {
            btn.IsEnabled = false;
            btn.Content = "Inviting...";

            await _callManager.InviteToCall(followerId);

            // Update UI
            var follower = _availableFollowers.First(f => f.Id == followerId);
            follower.Status = "Invite sent";
            
            btn.Content = "✓ Sent";
            btn.Background = new SolidColorBrush(Color.FromRgb(66, 183, 42));
        }
        catch (Exception ex)
        {
            btn.Content = "Failed";
            btn.Background = new SolidColorBrush(Color.FromRgb(240, 40, 73));
            Debug.WriteLine($"Invite failed: {ex.Message}");
        }
    }

    private async void OnBulkInvite(object sender, RoutedEventArgs e)
    {
        var selected = _availableFollowers.Where(f => f.IsSelected).Select(f => f.Id).ToList();
        if (selected.Count == 0) return;

        try
        {
            var response = await _http.PostAsJsonAsync(
                $"calls/{_callManager.CurrentCallId}/bulk-invite", 
                selected);

            var results = await response.Content.ReadFromJsonAsync<List<InviteResult>>();
            
            // Show results
            foreach (var result in results ?? new List<InviteResult>())
            {
                var follower = _availableFollowers.FirstOrDefault(f => f.Id == result.InviteeId);
                if (follower == null) continue;

                follower.Status = result.Success ? "Invite sent" : result.Error;
            }

            InviteFollowersList.Items.Refresh();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Bulk invite failed: {ex.Message}");
        }
    }

    // ========== INCOMING INVITE ==========

    private void OnIncomingInvite(object? sender, IncomingInviteData invite)
    {
        Dispatcher.Invoke(() =>
        {
            // Show incoming invite toast with accept/decline
            var toast = new InviteToast(invite);
            toast.OnAccept += async () => await AcceptInvite(invite.InviteId);
            toast.OnDecline += async () => await DeclineInvite(invite.InviteId);
            toast.Show();
        });
    }

    private async Task AcceptInvite(int inviteId)
    {
        await _callManager.AcceptCallInvite(inviteId);
    }

    private async Task DeclineInvite(int inviteId)
    {
        await _callManager.DeclineCallInvite(inviteId);
    }

    // ========== PARTICIPANT UPDATES ==========

    private void OnParticipantJoined(object? sender, ParticipantData participant)
    {
        Dispatcher.Invoke(() =>
        {
            var vm = new ParticipantViewModel
            {
                Id = participant.UserId,
                Name = participant.Name,
                Initial = participant.Name[0].ToString(),
                IsVerified = participant.IsVerified,
                VerifiedBadge = participant.IsVerified ? new VerifiedBadge() : null,
                Status = "In call",
                MicIcon = "🎤",
                CamIcon = "📹"
            };

            if (ParticipantsList.ItemsSource is List<ParticipantViewModel> list)
            {
                list.Add(vm);
                ParticipantsList.Items.Refresh();
            }

            ParticipantCount.Text = list?.Count.ToString() ?? "1";
        });
    }

    private void OnParticipantLeft(object? sender, int userId)
    {
        Dispatcher.Invoke(() =>
        {
            if (ParticipantsList.ItemsSource is List<ParticipantViewModel> list)
            {
                var participant = list.FirstOrDefault(p => p.Id == userId);
                if (participant != null)
                {
                    list.Remove(participant);
                    ParticipantsList.Items.Refresh();
                }
            }
        });
    }

    private void OnCloseInvite(object sender, RoutedEventArgs e)
    {
        InviteModal.Visibility = Visibility.Collapsed;
    }

    // ========== UPGRADE TO GROUP ==========

    private async void OnUpgradeToGroup(object sender, RoutedEventArgs e)
    {
        var result = await _callManager.UpgradeToGroupCall("RunApp Group Call");
        if (result)
        {
            InviteButton.Visibility = Visibility.Visible;
            ShowToast("Upgraded to group call. Invite followers now!");
        }
    }
}

// ViewModels
public class FollowerViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initial { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsVerified { get; set; }
    public UIElement? VerifiedBadge { get; set; }
    public bool IsSelected { get; set; }
}

public class ParticipantViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initial { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public UIElement? VerifiedBadge { get; set; }
    public string Status { get; set; } = string.Empty;
    public string MicIcon { get; set; } = "🎤";
    public string CamIcon { get; set; } = "📹";
}

// DTOs
public class FollowerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? LastActive { get; set; }
}

public class IncomingInviteData
{
    public int InviteId { get; set; }
    public int CallSessionId { get; set; }
    public int InviterId { get; set; }
    public string InviterName { get; set; } = string.Empty;
    public string InviterAvatar { get; set; } = string.Empty;
    public string CallType { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public bool IsGroupCall { get; set; }
    public string? GroupName { get; set; }
    public int ExpiresIn { get; set; }
}

public class InviteResult
{
    public int InviteeId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class ParticipantData
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime JoinedAt { get; set; }
}