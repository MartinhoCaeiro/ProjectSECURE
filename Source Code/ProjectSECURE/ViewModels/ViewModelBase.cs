using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectSECURE.ViewModels
{
    // Base class for all ViewModels, provides property change notification
    public class ViewModelBase : INotifyPropertyChanged
    {
        // Event for property change notification
        public event PropertyChangedEventHandler? PropertyChanged;

        // Notify listeners that a property value has changed
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
