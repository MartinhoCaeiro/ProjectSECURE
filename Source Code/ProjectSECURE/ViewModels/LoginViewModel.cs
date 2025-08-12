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
    public class LoginViewModel : INotifyPropertyChanged
    {
        private bool isWireGuardActive;
        private string username = string.Empty;
        private string password = string.Empty;
        private string errorMessage = string.Empty;

        public string Username
        {
            get => username;
            set { username = value; OnPropertyChanged(); }
        }
        public bool IsWireGuardActive
        {
            get => isWireGuardActive;
            set { isWireGuardActive = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => password;
            set { password = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(); }
        }

        public User? CurrentUser { get; private set; }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login);
            RegisterCommand = new RelayCommand(Register);
            IsWireGuardActive = false;
        }

        private void Login(object parameter)
        {
            if (UserRepository.ValidateLogin(Username, Password) is User user)
            {
                CurrentUser = user;
                ErrorMessage = "";

                if (parameter is Window window)
                {
                    var chatListView = new Views.ChatListView(user, IsWireGuardActive);
                    chatListView.Show();
                    window.Close();
                }
            }
            else
            {
                ErrorMessage = "Usuário ou senha inválidos.";
            }
        }

        private void Register(object parameter)
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
                ErrorMessage = "Usuário criado com sucesso! Faça login.";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
