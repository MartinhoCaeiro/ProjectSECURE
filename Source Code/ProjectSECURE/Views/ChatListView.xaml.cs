using ProjectSECURE.Models;
using ProjectSECURE.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace ProjectSECURE.Views
{
    // Code-behind for the chat list window
    public partial class ChatListView : Window
    {
        // ViewModel for chat list
        private readonly ChatListViewModel viewModel;
        // Indicates if WireGuard VPN is active
        private readonly bool isWireGuardActive;

        // Constructor initializes ViewModel and sets up event handlers
        public ChatListView(User user, bool isWireGuardActive)
        {
            InitializeComponent();
            this.isWireGuardActive = isWireGuardActive;
            viewModel = new ChatListViewModel(user, isWireGuardActive);
            DataContext = viewModel;
            viewModel.RequestNewChatWindow += ShowNewChatWindow;
        }

        // Opens the NewChatView window when requested by ViewModel
        private void ShowNewChatWindow()
        {
            var newChatWindow = new NewChatView(viewModel.CurrentUser)
            {
                Owner = this // inherit theme and resources
            };

            if (newChatWindow.ShowDialog() == true)
            {
                // Reload chat list after creating a new chat
                viewModel.GetType().GetMethod("LoadChats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(viewModel, null);
            }
        }

        // Opens the settings window
        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsView();
            settingsWindow.ShowDialog();
        }

        // Opens the selected chat in a new window on double-click
        private void Chat_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (viewModel.SelectedChat != null)
            {
                var chatView = new ChatView(viewModel.CurrentUser, viewModel.SelectedChat, isWireGuardActive);
                this.Hide();
                chatView.Closed += (s, args) => this.Show();
                chatView.Show();
            }
        }

        // Logs out and returns to the login window
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginView = new LoginView();
            loginView.Show();
            this.Close();
        }
    }
}
