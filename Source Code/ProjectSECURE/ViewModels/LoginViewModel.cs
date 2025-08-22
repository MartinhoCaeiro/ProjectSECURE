using ProjectSECURE.Data;
using ProjectSECURE.Models;
using ProjectSECURE.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System;

namespace ProjectSECURE.ViewModels
{
    // ViewModel for the login screen
    public class LoginViewModel : INotifyPropertyChanged
    {
        private bool isWireGuardActive;
        private string username = string.Empty;
        private string password = string.Empty;
        private string errorMessage = string.Empty;

        // Username entered by the user
        public string Username
        {
            get => username;
            set { username = value; OnPropertyChanged(); }
        }
        // Indicates if WireGuard VPN is active
        public bool IsWireGuardActive
        {
            get => isWireGuardActive;
            set { isWireGuardActive = value; OnPropertyChanged(); }
        }

        // Password entered by the user
        public string Password
        {
            get => password;
            set { password = value; OnPropertyChanged(); }
        }

        // Error message to display in the UI
        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(); }
        }

        // The currently logged-in user
        public User? CurrentUser { get; private set; }

        // Command to perform login
        public ICommand LoginCommand { get; }
        // Command to perform registration
        public ICommand RegisterCommand { get; }

        // Constructor sets up commands and initial VPN status
        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login);
            RegisterCommand = new RelayCommand(Register);
            IsWireGuardActive = false;
        }

        // Attempt to log in with the provided credentials
        private void Login(object? parameter)
        {
            if (UserRepository.ValidateLogin(Username, Password) is User user)
            {
                CurrentUser = user;
                ErrorMessage = "";

                // If called from a window, open chat list and close login window
                if (parameter is Window window)
                {
                    var chatListView = new Views.ChatListView(user, IsWireGuardActive);
                    chatListView.Show();
                    window.Close();
                }
            }
            else
            {
                ErrorMessage = "Nome ou senha inválidos.";
            }
        }

        // Register a new user with the provided credentials
        private void Register(object? parameter)
        {
            try
            {
                var user = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Name = Username,
                    Password = Password
                };
                UserRepository.CreateUser(user);
                // Upload database after user creation
                _ = Services.DbSyncService.UploadDatabaseAsync();
                ErrorMessage = "Utilizador criado com sucesso! Por favor, faça login.";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        // Property change notification
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
