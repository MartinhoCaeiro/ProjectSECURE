using ProjectSECURE.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Linq;

namespace ProjectSECURE.Views
{
    // Code-behind for the login window
    public partial class LoginView : Window
    {
        // Constructor sets up DataContext and event handlers
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
            Loaded += async (_, __) => await UpdateWireGuardStatusAsync();
            this.Closed += LoginView_Closed;
        }

        // Updates the WireGuard VPN status and updates the UI and ViewModel
        private async Task UpdateWireGuardStatusAsync()
        {
            WireGuardStatusButton.Content = "WireGuard: a verificar…";
            var active = await Task.Run(IsWireGuardInterfaceUp);
            WireGuardStatusButton.Content = active ? "O WireGuard está ativo" : "Por favor, inicie o WireGuard";
            if (DataContext is ViewModels.LoginViewModel vm)
            {
                vm.IsWireGuardActive = active;
            }
        }

        // Checks if the WireGuard network interface is up
        public static bool IsWireGuardInterfaceUp()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Any(ni =>
                    ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.Name.Contains("WireGuard", System.StringComparison.OrdinalIgnoreCase) ||
                     ni.Description.Contains("WireGuard", System.StringComparison.OrdinalIgnoreCase)));
        }

        // Opens the WireGuard configuration window and refreshes VPN status after closing
        private async void WireGuardStatusButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new WireGuardConfigView { Owner = this };
            win.ShowDialog();
            await UpdateWireGuardStatusAsync();
        }

        // Handles window close event; opens chat list if login was successful
        private void LoginView_Closed(object? sender, EventArgs e)
        {
            if (this.DialogResult == true)
            {
                if (DataContext is LoginViewModel vm && vm.CurrentUser != null)
                {
                    // Open main chat list window, passing logged-in user and VPN status
                    var chatListView = new ChatListView(vm.CurrentUser, vm.IsWireGuardActive);
                    chatListView.Show();
                }
            }
        }

        // Updates the ViewModel's password property when the password box changes
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                var passwordBox = sender as PasswordBox;
                if (passwordBox != null)
                {
                    vm.Password = passwordBox.Password;
                }
            }
        }
    }
}
