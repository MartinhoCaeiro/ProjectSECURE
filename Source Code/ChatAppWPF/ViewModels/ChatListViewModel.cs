using ChatAppWPF.Data;
using ChatAppWPF.Models;
using ChatAppWPF.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ChatAppWPF.ViewModels
{
    public class ChatListViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Chat> Chats { get; set; } = new();
        public User CurrentUser => currentUser;
        private readonly User currentUser;

        public ICommand NewChatCommand { get; }

        public Chat? SelectedChat { get; set; }

        public ChatListViewModel(User user)
        {
            currentUser = user;
            LoadChats();

            NewChatCommand = new RelayCommand(_ => NewChat());
        }

        private void LoadChats()
        {
            Chats.Clear();
            var chats = ChatRepository.GetChatsForUser(currentUser.UserId);
            foreach (var chat in chats)
                Chats.Add(chat);
        }

        private void NewChat()
        {
            var name = PromptForChatName();
            if (string.IsNullOrWhiteSpace(name)) return;

            var newChat = ChatRepository.CreateNewChat(name, currentUser.UserId);
            if (newChat != null)
                Chats.Add(newChat);
        }

        private string PromptForChatName()
        {
            return Microsoft.VisualBasic.Interaction.InputBox("Nome do novo chat:", "Novo Chat", "Grupo");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
