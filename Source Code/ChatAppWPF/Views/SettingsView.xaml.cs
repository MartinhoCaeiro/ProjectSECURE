using System.Windows;
using System.Windows.Controls;

namespace ChatAppWPF.Views
{
    public partial class SettingsView : Window
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void ThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            var selected = ((ComboBoxItem)e.AddedItems[0]).Content.ToString();

            string theme = selected == "Escuro" ? "Dark" : "Light";

            ((App)Application.Current).ApplyTheme(theme);
        }
    }
}
