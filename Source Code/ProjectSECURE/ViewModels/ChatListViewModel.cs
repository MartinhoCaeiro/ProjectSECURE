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
    // ViewModel for the chat list screen
    public class ChatListViewModel : INotifyPropertyChanged
    {
        // List of chats for the current user
        public ObservableCollection<Chat> Chats { get; set; } = new();
        // Current logged-in user
        public User CurrentUser => currentUser;
        private readonly User currentUser;

        // Command to create a new chat
        public ICommand NewChatCommand { get; }

        // Currently selected chat
        public Chat? SelectedChat { get; set; }

        // Event for requesting the view to open the new chat window
        public event Action? RequestNewChatWindow;

        // Indicates if WireGuard VPN is active
        public bool IsWireGuardActive { get; }

        // Constructor initializes user, VPN status, loads chats, and sets up command
        public ChatListViewModel(User user, bool isWireGuardActive)
        {
            currentUser = user;
            IsWireGuardActive = isWireGuardActive;
            LoadChats();
            NewChatCommand = new RelayCommand(_ => RequestNewChatWindow?.Invoke());
        }

        // Load chats for the current user from the repository
        private void LoadChats()
        {
            Chats.Clear();
            var chats = ChatRepository.GetChatsForUser(currentUser.UserId);
            foreach (var chat in chats)
                Chats.Add(chat);
        }

        // Refresh the chat list (e.g., after creating a new chat)
        public void RefreshChats()
        {
            LoadChats();
        }

        // Property change notification
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
