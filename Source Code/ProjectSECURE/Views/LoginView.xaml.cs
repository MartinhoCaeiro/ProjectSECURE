using ProjectSECURE.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Linq;

namespace ProjectSECURE.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
            Loaded += async (_, __) => await UpdateWireGuardStatusAsync();

            this.Closed += LoginView_Closed;
        }

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

        public static bool IsWireGuardInterfaceUp()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Any(ni =>
                    ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.Name.Contains("WireGuard", System.StringComparison.OrdinalIgnoreCase) ||
                     ni.Description.Contains("WireGuard", System.StringComparison.OrdinalIgnoreCase)));
        }

        private async void WireGuardStatusButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new WireGuardConfigView { Owner = this };
            win.ShowDialog();
            await UpdateWireGuardStatusAsync();
        }

        private void LoginView_Closed(object? sender, EventArgs e)
        {
            if (this.DialogResult == true)
            {
                if (DataContext is LoginViewModel vm && vm.CurrentUser != null)
                {
                    // Abrir janela principal, passando o usuário logado e status WireGuard
                    var chatListView = new ChatListView(vm.CurrentUser, vm.IsWireGuardActive);
                    chatListView.Show();
                }
            }
        }

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
