using ProjectSECURE.Models;
using ProjectSECURE.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ProjectSECURE.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();

            this.Closed += LoginView_Closed;
        }

        private void LoginView_Closed(object? sender, EventArgs e)
        {
            if (this.DialogResult == true)
            {
                if (DataContext is LoginViewModel vm && vm.CurrentUser != null)
                {
                    // Abrir janela principal, passando o usuário logado
                    var chatListView = new ChatListView(vm.CurrentUser);
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
