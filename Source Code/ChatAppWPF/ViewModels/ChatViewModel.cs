using ChatAppWPF.Data;
using ChatAppWPF.Models;
using ChatAppWPF.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.Generic;

namespace ChatAppWPF.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<MessageBubble> Messages { get; set; } = new();
        public string NewMessage { get; set; }

        public ICommand SendCommand { get; }

        private readonly User currentUser;
        private readonly Chat currentChat;
        private readonly Dictionary<string, string> userNames = new();

        public ChatViewModel(User user, Chat chat)
        {
            currentUser = user;
            currentChat = chat;

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


        private void SendMessage()
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
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MessageBubble
    {
        public string Content { get; set; }
        public bool IsSentByUser { get; set; }
        public string SenderName { get; set; }
    }

}
