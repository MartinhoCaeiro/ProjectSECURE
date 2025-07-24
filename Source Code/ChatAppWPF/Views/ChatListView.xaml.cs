using ChatAppWPF.Models;
using ChatAppWPF.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace ChatAppWPF.Views
{
    public partial class ChatListView : Window
    {
        private readonly ChatListViewModel viewModel;

        public ChatListView(User user)
        {
            InitializeComponent();
            viewModel = new ChatListViewModel(user);
            DataContext = viewModel;

            viewModel.RequestNewChatWindow += ShowNewChatWindow;
        }

        private void ShowNewChatWindow()
        {
            var newChatWindow = new NewChatView(viewModel.CurrentUser)
            {
                Owner = this // herda o estilo e os recursos do tema
            };

            if (newChatWindow.ShowDialog() == true)
            {
                // Recarrega a lista de chats após criar um novo
                viewModel.GetType().GetMethod("LoadChats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(viewModel, null);
            }
        }


        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsView();
            settingsWindow.ShowDialog();
        }

        private void Chat_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (viewModel.SelectedChat != null)
            {
                var chatView = new ChatView(viewModel.CurrentUser, viewModel.SelectedChat);
                chatView.Show();
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginView = new LoginView();
            loginView.Show();
            this.Close();
        }
    }
}
