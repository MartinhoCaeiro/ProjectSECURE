using System.Windows;
using ChatAppWPF.Models;
using ChatAppWPF.ViewModels;

namespace ChatAppWPF.Views
{
    public partial class ChatView : Window
    {
        public ChatView(Chat chat)
        {
            InitializeComponent();
            DataContext = new ChatViewModel(chat);
        }

    }
}
