using System.Windows;
using ProjectSECURE.Models;
using ProjectSECURE.ViewModels;

namespace ProjectSECURE.Views
{
    public partial class ChatView : Window
    {
        public ChatView(User user, Chat chat, bool isWireGuardActive)
        {
            InitializeComponent();
            DataContext = new ChatViewModel(user, chat, isWireGuardActive);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Fecha o ChatView e retorna à janela anterior (ChatListView)
        }

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
