using System.Windows;
using ChatAppWPF.Models;
using ChatAppWPF.ViewModels;

namespace ChatAppWPF.Views
{
    public partial class ChatListView : Window
    {
        public ChatListView()
        {
            InitializeComponent();
            this.DataContext = new ChatListViewModel(); // <-- Adiciona esta linha
        }

        private void NewChat_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Criar novo chat - ainda não implementado.");
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var settingsView = new SettingsView();
            settingsView.ShowDialog();
        }

        private void Chat_DoubleClick(object sender, RoutedEventArgs e)
        {
            var chat = (sender as System.Windows.Controls.ListBox)?.SelectedItem as Chat;
            if (chat != null)
            {
                var chatWindow = new ChatView(chat);
                chatWindow.Show();
            }
        }
    }
}
