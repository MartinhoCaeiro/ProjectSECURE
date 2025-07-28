using System.Windows;
using System.Windows.Controls;

namespace ProjectSECURE.Views
{
    public partial class SettingsView : Window
    {
        public SettingsView()
        {
            InitializeComponent();

            // Obter o tema atual da aplicação
            string currentTheme = ((App)Application.Current).CurrentTheme;

            // Atualizar seleção da ComboBox conforme o tema
            ThemeComboBox.SelectedIndex = currentTheme == "Dark" ? 1 : 0;
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
