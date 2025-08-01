using ProjectSECURE.Data;
using ProjectSECURE.Models;
using ProjectSECURE.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProjectSECURE.ViewModels
{
    public class ChatListViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Chat> Chats { get; set; } = new();
        public User CurrentUser => currentUser;
        private readonly User currentUser;

        public ICommand NewChatCommand { get; }

        public Chat? SelectedChat { get; set; }

        // NOVO: evento que a View pode escutar para abrir a janela
        public event Action? RequestNewChatWindow;

        public ChatListViewModel(User user)
        {
            currentUser = user;
            LoadChats();

            // NOVO: aciona evento para pedir à View que abra a janela
            NewChatCommand = new RelayCommand(_ => RequestNewChatWindow?.Invoke());
        }

        private void LoadChats()
        {
            Chats.Clear();
            var chats = ChatRepository.GetChatsForUser(currentUser.UserId);
            foreach (var chat in chats)
                Chats.Add(chat);
        }

        // NOVO: método público para refrescar a lista depois de criar um chat
        public void RefreshChats()
        {
            LoadChats();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
