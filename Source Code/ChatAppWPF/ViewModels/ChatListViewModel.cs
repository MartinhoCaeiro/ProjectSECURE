using System.Collections.ObjectModel;
using System.Windows.Input;
using ChatAppWPF.Models;

namespace ChatAppWPF.ViewModels
{
    public class ChatListViewModel
    {
        public ObservableCollection<Chat> Chats { get; set; }

        private Chat selectedChat;
        public Chat SelectedChat
        {
            get => selectedChat;
            set => selectedChat = value;
        }

        public ICommand NewChatCommand { get; }

        public ChatListViewModel()
        {
            Chats = new ObservableCollection<Chat>
            {
                new Chat { Name = "João" },
                new Chat { Name = "Maria" },
                new Chat { Name = "Pedro" }
            };

            NewChatCommand = new RelayCommand(NewChat);
        }

        private void NewChat()
        {
            // Implementar a criação de novo chat ou mostrar mensagem
            System.Windows.MessageBox.Show("Criar novo chat - ainda não implementado.");
        }
    }
}
