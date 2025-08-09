using ProjectSECURE.Models;
using ProjectSECURE.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace ProjectSECURE.Views
{
    public partial class ChatListView : Window
    {
        private readonly ChatListViewModel viewModel;
        private readonly bool isWireGuardActive;

        public ChatListView(User user, bool isWireGuardActive)
        {
            InitializeComponent();
            this.isWireGuardActive = isWireGuardActive;
            viewModel = new ChatListViewModel(user, isWireGuardActive);
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
                var chatView = new ChatView(viewModel.CurrentUser, viewModel.SelectedChat, isWireGuardActive);
                this.Hide();
                chatView.Closed += (s, args) => this.Show();
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
