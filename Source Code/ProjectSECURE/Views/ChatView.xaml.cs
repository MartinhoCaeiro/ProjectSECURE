using System.Windows;
using ProjectSECURE.Models;
using ProjectSECURE.ViewModels;

namespace ProjectSECURE.Views
{
    public partial class ChatView : Window
    {
        public ChatView(User user, Chat chat)
        {
            InitializeComponent();
            DataContext = new ChatViewModel(user, chat);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Fecha o ChatView e retorna à janela anterior (ChatListView)
        }


    }
}
