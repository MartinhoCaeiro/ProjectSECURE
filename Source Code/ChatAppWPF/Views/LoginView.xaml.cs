using System.Windows;
using System.Windows.Controls; // <-- necessário para PasswordBox
using ChatAppWPF.ViewModels;

namespace ChatAppWPF.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();

            var vm = new LoginViewModel();
            DataContext = vm;

            vm.LoginSucceeded += (s, e) =>
            {
                var mainWindow = new Views.ChatListView();

                // Define a janela principal como a nova janela
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();

                // Fecha o login
                this.Close();
            };
        }


        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = (sender as PasswordBox)?.Password ?? string.Empty;
            }
        }
    }
}
