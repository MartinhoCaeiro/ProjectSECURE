using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System;


namespace ChatAppWPF.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string username = string.Empty;
        public string Username
        {
            get => username;
            set { username = value; OnPropertyChanged(nameof(Username)); }
        }

        private string password = string.Empty;
        public string Password
        {
            get => password;
            set { password = value; OnPropertyChanged(nameof(Password)); }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        }

        public event EventHandler? LoginSucceeded;

        private void ExecuteLogin()
        {
            if (Username == "user" && Password == "1234")
            {
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                MessageBox.Show("Credenciais inválidas.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private bool CanExecuteLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
