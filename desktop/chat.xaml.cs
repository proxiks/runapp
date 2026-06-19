using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;

namespace RunApp.Desktop.Views;

public partial class ChatView : UserControl
{
    private HubConnection? _chatHub;
    private readonly HttpClient _http;
    private readonly LyfronCrypto _crypto;
    private int _currentConversationId;
    private int _currentUserId;
    private DispatcherTimer? _typingTimer;

    public ChatView()
    {
        InitializeComponent();
        _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/api/"),
            DefaultRequestHeaders =
            {
                { "Authorization", $"Bearer {Properties.Settings.Default.AuthToken}" }
            }
        };
        _crypto = new LyfronCrypto();
        _currentUserId = GetUserIdFromToken();

        InitializeHub();
        LoadConversations();
    }

    private async void InitializeHub()
    {
        _chatHub = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/hubs/chat", options =>
            {
                options.AccessTokenProvider = () => 
                    Task.FromResult(Properties.Settings.Default.AuthToken)!;
            })
            .WithAutomaticReconnect()
            .Build();

        // Handle incoming messages
        _chatHub.On<MessagePayload>("ReceiveMessage", msg =>
        {
            Dispatcher.Invoke(() =>
            {
                if (msg.ConversationId == _currentConversationId)
                {
                    AddMessageToUi(msg);
                    ScrollToBottom();
                }
                else
                {
                    // Show notification
                    ShowNotification(msg);
                }
            });
        });

        // Handle typing
        _chatHub.On<TypingPayload>("UserTyping", typing =>
        {
            if (typing.ConversationId == _currentConversationId)
            {
                Dispatcher.Invoke(() =>
                {
                    TypingIndicator.Visibility = Visibility.Visible;
                    HideTypingAfterDelay();
                });
            }
        });

        // Handle read receipts
        _chatHub.On<ReadReceiptPayload>("MessagesRead", receipt =>
        {
            Dispatcher.Invoke(() => UpdateReadStatus(receipt));
        });

        await _chatHub.StartAsync();
    }

    private async void LoadConversations()
    {
        try
        {
            var conversations = await _http.GetFromJsonAsync<List<ConversationDto>>("chat/conversations");
            if (conversations == null) return;

            ConversationList.ItemsSource = conversations.Select(c => new ConversationViewModel
            {
                Id = c.Id,
                DisplayName = c.Type == "direct" ? c.OtherParticipant?.Name : c.Name,
                OtherParticipantId = c.OtherParticipant?.Id,
                IsVerifiedOnly = c.Type == "verified_only",
                VerifiedBadge = c.OtherParticipant?.IsVerified == true ? new VerifiedBadge() : null,
                LastMessagePreview = DecryptPreview(c.LastMessage),
                LastMessageTime = FormatTime(c.LastMessage?.CreatedAt),
                UnreadCount = c.UnreadCount,
                Avatar = c.OtherParticipant?.Name?[0].ToString() ?? "?"
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load conversations: {ex.Message}");
        }
    }

    private async void OnConversationSelected(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.DataContext is not ConversationViewModel conv) return;

        _currentConversationId = conv.Id;
        
        // Show chat area
        EmptyState.Visibility = Visibility.Collapsed;
        ChatHeader.Visibility = Visibility.Visible;
        MessagesScroll.Visibility = Visibility.Visible;
        InputArea.Visibility = Visibility.Visible;

        // Update header
        HeaderAvatar.Text = conv.Avatar;
        HeaderName.Text = conv.DisplayName;
        HeaderVerifiedBadge.Content = conv.VerifiedBadge;
        HeaderStatus.Text = "online"; // Get real status from hub

        // Load messages
        await LoadMessages(conv.Id);
    }

    private async Task LoadMessages(int conversationId)
    {
        try
        {
            var messages = await _http.GetFromJsonAsync<List<MessageDto>>($"chat/conversations/{conversationId}/messages");
            if (messages == null) return;

            MessagesList.ItemsSource = messages.Select(m => new MessageViewModel
            {
                Id = m.Id,
                Content = m.Content,
                DecryptedContent = DecryptMessage(m.Content, m.ContentIv),
                IsOwn = m.SenderId == _currentUserId,
                Alignment = m.SenderId == _currentUserId ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Time = FormatTime(m.CreatedAt),
                ReadStatus = m.IsRead ? "✓✓" : "✓",
                ReplyTo = m.ReplyToId,
                MediaUrl = m.MediaUrl,
                MediaThumbnail = m.MediaThumbnail
            });

            ScrollToBottom();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load messages: {ex.Message}");
        }
    }

    private async void OnSendMessage(object sender, RoutedEventArgs e)
    {
        var text = MessageInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Encrypt with Lyfron
        var (encrypted, iv) = _crypto.Encrypt(text);

        // Send via SignalR
        if (_chatHub?.State == HubConnectionState.Connected)
        {
            await _chatHub.InvokeAsync("SendMessage", new
            {
                conversationId = _currentConversationId,
                content = Convert.ToBase64String(encrypted),
                contentIv = Convert.ToBase64String(iv),
                type = "text"
            });
        }

        MessageInput.Clear();
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
        {
            e.Handled = true;
            OnSendMessage(sender, e);
            return;
        }

        // Send typing indicator
        SendTypingIndicator();
    }

    private void SendTypingIndicator()
    {
        _typingTimer?.Stop();
        _typingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _typingTimer.Tick += async (s, e) =>
        {
            _typingTimer.Stop();
            if (_chatHub?.State == HubConnectionState.Connected)
            {
                await _chatHub.InvokeAsync("Typing", _currentConversationId);
            }
        };
        _typingTimer.Start();
    }

    private void HideTypingAfterDelay()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, e) =>
        {
            TypingIndicator.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }

    private void AddMessageToUi(MessagePayload msg)
    {
        var viewModel = new MessageViewModel
        {
            Id = msg.Id,
            DecryptedContent = DecryptMessage(Convert.FromBase64String(msg.Content), 
                string.IsNullOrEmpty(msg.ContentIv) ? null : Convert.FromBase64String(msg.ContentIv)),
            IsOwn = msg.SenderId == _currentUserId,
            Alignment = msg.SenderId == _currentUserId ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Time = "Just now",
            ReadStatus = "✓"
        };

        // Add to list
        if (MessagesList.ItemsSource is List<MessageViewModel> list)
        {
            list.Add(viewModel);
            MessagesList.Items.Refresh();
        }
    }

    private string DecryptMessage(byte[] encrypted, byte[]? iv)
    {
        if (iv == null) return "[Encrypted]";
        return _crypto.Decrypt(encrypted, iv);
    }

    private string DecryptPreview(MessageDto? msg)
    {
        if (msg == null) return "No messages yet";
        try
        {
            return DecryptMessage(Convert.FromBase64String(msg.Content),
                string.IsNullOrEmpty(msg.ContentIv) ? null : Convert.FromBase64String(msg.ContentIv));
        }
        catch
        {
            return "Message";
        }
    }

    private void ScrollToBottom()
    {
        MessagesScroll.ScrollToEnd();
    }

    private void ShowNotification(MessagePayload msg)
    {
        // Show desktop notification
        var toast = new NotificationToast(new NotificationPayload
        {
            Title = msg.SenderName,
            Message = msg.Type == "text" ? "New message" : "Media",
            DeepLink = $"runapp://chat/{msg.ConversationId}"
        });
        toast.Show();
    }

    private void OnAttachFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Images|*.jpg;*.jpeg;*.png;*.gif|Videos|*.mp4;*.mov|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            // Upload file then send message with mediaUrl
            _ = UploadAndSendMedia(dialog.FileName);
        }
    }

    private async Task UploadAndSendMedia(string filePath)
    {
        // Implement file upload to server/CDN
        // Then send message with mediaUrl
    }

    private void OnNewMessage(object sender, RoutedEventArgs e)
    {
        // Show new message dialog to search users
        var dialog = new NewMessageDialog();
        if (dialog.ShowDialog() == true && dialog.SelectedUserId.HasValue)
        {
            _ = _chatHub?.InvokeAsync("CreateDirectConversation", dialog.SelectedUserId.Value);
        }
    }

    private void OnSearchConversations(object sender, TextChangedEventArgs e)
    {
        // Filter conversation list
    }

    private static string FormatTime(DateTime? date)
    {
        if (!date.HasValue) return "";
        var span = DateTime.UtcNow - date.Value;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours}h";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d";
        return date.Value.ToString("MMM d");
    }

    private int GetUserIdFromToken()
    {
        // Parse JWT token to get user ID
        var token = Properties.Settings.Default.AuthToken;
        // Implement JWT parsing
        return 1; // Placeholder
    }
}

// ViewModels
public class ConversationViewModel
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int? OtherParticipantId { get; set; }
    public bool IsVerifiedOnly { get; set; }
    public UIElement? VerifiedBadge { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public string LastMessageTime { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public string Avatar { get; set; } = "?";
}

public class MessageViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string DecryptedContent { get; set; } = string.Empty;
    public bool IsOwn { get; set; }
    public HorizontalAlignment Alignment { get; set; }
    public string Time { get; set; } = string.Empty;
    public string ReadStatus { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaThumbnail { get; set; }
}

// DTOs
public class ConversationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Name { get; set; }
    public UserDto? OtherParticipant { get; set; }
    public MessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
}

public class MessageDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ContentIv { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaThumbnail { get; set; }
    public string? ReplyToId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class MessagePayload
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContentIv { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? ReplyToId { get; set; }
}

public class TypingPayload
{
    public int UserId { get; set; }
    public int ConversationId { get; set; }
}

public class ReadReceiptPayload
{
    public int UserId { get; set; }
    public int ConversationId { get; set; }
    public int LastMessageId { get; set; }
}