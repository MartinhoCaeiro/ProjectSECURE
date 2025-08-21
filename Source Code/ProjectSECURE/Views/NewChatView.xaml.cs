using ProjectSECURE.Data;
using ProjectSECURE.Models;
using System.Collections.Generic;
using System.Windows;

namespace ProjectSECURE.Views
{
    // Code-behind for the new chat creation window
    public partial class NewChatView : Window
    {
        // The current logged-in user
        private readonly User currentUser;

        // Constructor sets up the user and loads the user list
        public NewChatView(User user)
        {
            InitializeComponent();
            currentUser = user;
            LoadUsers();
        }

        // Loads all users into the participants list and selects the current user by default
        private void LoadUsers()
        {
            var allUsers = UserRepository.GetAllUsers();
            UsersListBox.ItemsSource = allUsers;
            UsersListBox.DisplayMemberPath = "Name";
            UsersListBox.SelectedItems.Add(currentUser); // Select current user by default
        }

        // Handles the create chat button click
        private void CreateChat_Click(object sender, RoutedEventArgs e)
        {
            var chatName = ChatNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(chatName))
            {
                MessageBox.Show("Por favor escolha um nome.");
                return;
            }

            var selectedUsers = new List<User>();
            foreach (User user in UsersListBox.SelectedItems)
            {
                selectedUsers.Add(user);
            }

            if (selectedUsers.Count < 2)
            {
                MessageBox.Show("Selecione pelo menos dois participantes.");
                return;
            }

            // Create the chat and add participants
            string chatId = ChatRepository.CreateChat(chatName, currentUser.UserId, selectedUsers);

            // Upload database after chat creation
            _ = ProjectSECURE.Services.DbSyncService.UploadDatabaseAsync();

            MessageBox.Show("Chat criado com sucesso!");
            this.DialogResult = true;
            this.Close();
        }
    }
}
