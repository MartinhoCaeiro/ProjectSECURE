using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using ChatAppWPF.Models;
using ChatAppWPF.Utils;

namespace ChatAppWPF.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Message> Messages { get; set; } = new();
        private string _newMessage = "";

        public string NewMessage
        {
            get => _newMessage;
            set
            {
                _newMessage = value;
                OnPropertyChanged(nameof(NewMessage));
            }
        }

        public ICommand SendCommand { get; }

        private readonly Chat _chat;

        public ChatViewModel(Chat chat)
        {
            _chat = chat;
            SendCommand = new RelayCommand(Send);
        }

        private void Send()
        {
            if (!string.IsNullOrWhiteSpace(NewMessage))
            {
                Messages.Add(new Message { Content = NewMessage });

                // Escrever no terminal
                System.Console.WriteLine($"Mensagem enviada: {NewMessage}");

                NewMessage = "";
                OnPropertyChanged(nameof(NewMessage));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
