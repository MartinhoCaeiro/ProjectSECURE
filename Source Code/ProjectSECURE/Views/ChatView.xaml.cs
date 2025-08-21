using System.Windows;
using ProjectSECURE.Models;
using ProjectSECURE.ViewModels;

namespace ProjectSECURE.Views
{
    // Code-behind for the chat conversation window
    public partial class ChatView : Window
    {
        // Constructor sets up the ViewModel for the chat
        public ChatView(User user, Chat chat, bool isWireGuardActive)
        {
            InitializeComponent();
            DataContext = new ChatViewModel(user, chat, isWireGuardActive);
        }

        // Handles the Back button click to close the chat window and return to chat list
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Scrolls the message list to the bottom when loaded or when new messages arrive
        private void MessagesListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox != null)
            {
                // Scroll to bottom on load
                if (listBox.Items.Count > 0)
                    listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                // Scroll to bottom on collection change
                ((System.Collections.Specialized.INotifyCollectionChanged)listBox.Items).CollectionChanged += (s, args) =>
                {
                    if (listBox.Items.Count > 0)
                        listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                };
            }
        }
    }
}
