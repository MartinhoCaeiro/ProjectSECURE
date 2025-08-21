using System;
using ProjectSECURE.Data;
using ProjectSECURE.Models;
using ProjectSECURE.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ProjectSECURE.ViewModels
{
    // ViewModel for the chat conversation screen
    public class ChatViewModel : ViewModelBase
    {
        // Timer for auto-refreshing messages
        private readonly DispatcherTimer refreshTimer;
        private bool isWireGuardActive = true;

        // List of message bubbles in the chat
        public ObservableCollection<MessageBubble> Messages { get; set; } = new();
        // Text of the new message being composed
        public string NewMessage { get; set; } = string.Empty;

        // Indicates if WireGuard VPN is active
        public bool IsWireGuardActive
        {
            get => isWireGuardActive;
            set { isWireGuardActive = value; OnPropertyChanged(); }
        }

        // Command to send a message
        public ICommand SendCommand { get; }
        // Command to manually refresh messages
        public ICommand RefreshCommand { get; }

        private readonly User currentUser;
        private readonly Chat currentChat;
        // Dictionary mapping user IDs to names
        private readonly Dictionary<string, string> userNames = new();

        // Constructor initializes user, chat, VPN status, loads messages, and sets up commands
        public ChatViewModel(User user, Chat chat, bool isWireGuardActive)
        {
            // Start auto-refresh timer (every 10 seconds)
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(10);
            refreshTimer.Tick += async (s, e) => await RefreshMessagesAsync();
            refreshTimer.Start();

            currentUser = user;
            currentChat = chat;
            IsWireGuardActive = isWireGuardActive;
            LoadMessages();
            SendCommand = new RelayCommand(_ => SendMessage());
            RefreshCommand = new RelayCommand(async _ => await RefreshMessagesAsync());
        }

        // Refresh messages from the database and reload them
        private async Task RefreshMessagesAsync()
        {
            await Services.DbSyncService.PeriodicDownloadAndReloadAsync();
            LoadMessages();
        }

        // Load all user names for display in chat bubbles
        private void LoadUserNames()
        {
            var users = UserRepository.GetAllUsers();
            foreach (var u in users)
                userNames[u.UserId] = u.Name ?? "Unknown";
        }

        // Load all messages for the current chat
        private void LoadMessages()
        {
            Messages.Clear();
            LoadUserNames();

            var rawMessages = MessageRepository.LoadMessages(currentChat.ChatId);

            foreach (var msg in rawMessages)
            {
                Messages.Add(new MessageBubble
                {
                    Content = msg.Content,
                    IsSentByUser = msg.SenderUserId == currentUser.UserId,
                    SenderName = userNames.TryGetValue(msg.SenderUserId ?? "", out var name) ? name : "Unknown"
                });
            }
        }

        // Send a new message and update the chat
        private async void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(NewMessage))
                return;

            MessageRepository.SendMessage(currentChat.ChatId, currentUser.UserId, NewMessage);

            Messages.Add(new MessageBubble
            {
                Content = NewMessage,
                IsSentByUser = true
            });

            NewMessage = string.Empty;
            OnPropertyChanged(nameof(NewMessage));

            await Services.DbSyncService.UploadDatabaseAsync();

        }
    }

    // Model for a chat message bubble
    public class MessageBubble
    {
        public string? Content { get; set; }
        public bool IsSentByUser { get; set; }
        public string? SenderName { get; set; }
    }

}
