using ProjectSECURE.Data;
using ProjectSECURE.Models;
using System.Collections.Generic;
using System.Windows;

namespace ProjectSECURE.Views
{
    public partial class NewChatView : Window
    {
        private readonly User currentUser;

        public NewChatView(User user)
        {
            InitializeComponent();
            currentUser = user;
            LoadUsers();
        }

        private void LoadUsers()
        {
            var allUsers = UserRepository.GetAllUsers();
            UsersListBox.ItemsSource = allUsers;
            UsersListBox.DisplayMemberPath = "Name";
            UsersListBox.SelectedItems.Add(currentUser); // Selecionar o utilizador atual por defeito
        }

        private void CreateChat_Click(object sender, RoutedEventArgs e)
        {
            var chatName = ChatNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(chatName))
            {
                MessageBox.Show("Insira o nome do chat.");
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

            // Criar o chat e participantes
            string chatId = ChatRepository.CreateChat(chatName, currentUser.UserId, selectedUsers);

            // Upload database after chat creation
            _ = ProjectSECURE.Services.DbSyncService.UploadDatabaseAsync();

            MessageBox.Show("Chat criado com sucesso!");
            this.DialogResult = true;
            this.Close();
        }
    }
}
