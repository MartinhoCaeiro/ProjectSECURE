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
