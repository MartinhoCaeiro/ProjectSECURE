using ProjectSECURE.Data;
using ProjectSECURE.Models;
using ProjectSECURE.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Collections.Generic;

namespace ProjectSECURE.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        private bool isWireGuardActive = true;
        public ObservableCollection<MessageBubble> Messages { get; set; } = new();
        public string NewMessage { get; set; } = string.Empty;
        public bool IsWireGuardActive
        {
            get => isWireGuardActive;
            set { isWireGuardActive = value; OnPropertyChanged(); }
        }

        public ICommand SendCommand { get; }

        private readonly User currentUser;
        private readonly Chat currentChat;
        private readonly Dictionary<string, string> userNames = new();

        public ChatViewModel(User user, Chat chat, bool isWireGuardActive)
        {
            currentUser = user;
            currentChat = chat;
            IsWireGuardActive = isWireGuardActive;
            LoadMessages();
            SendCommand = new RelayCommand(_ => SendMessage());
        }


        private void LoadUserNames()
        {
            var users = UserRepository.GetAllUsers();
            foreach (var u in users)
                userNames[u.UserId] = u.Name ?? "Desconhecido";
        }

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
                    SenderName = userNames.TryGetValue(msg.SenderUserId ?? "", out var name) ? name : "Desconhecido"
                });
            }
        }


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

        // Inherited OnPropertyChanged and PropertyChanged from ViewModelBase
    }

    public class MessageBubble
    {
        public string? Content { get; set; }
        public bool IsSentByUser { get; set; }
        public string? SenderName { get; set; }
    }

}
