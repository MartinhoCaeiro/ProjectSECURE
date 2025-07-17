using ChatAppWPF.Data;
using ChatAppWPF.Models;
using ChatAppWPF.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ChatAppWPF.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<MessageBubble> Messages { get; set; } = new();
        public string NewMessage { get; set; }

        public ICommand SendCommand { get; }

        private readonly User currentUser;
        private readonly Chat currentChat;

        public ChatViewModel(User user, Chat chat)
        {
            currentUser = user;
            currentChat = chat;

            LoadMessages();

            SendCommand = new RelayCommand(_ => SendMessage());

        }

        private void LoadMessages()
        {
            Messages.Clear();
            var rawMessages = MessageRepository.LoadMessages(currentChat.ChatId);

            foreach (var msg in rawMessages)
            {
                Messages.Add(new MessageBubble
                {
                    Content = msg.Content,
                    IsSentByUser = msg.SenderUserId == currentUser.UserId
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
    }
}
