using System.Windows;
using System.Windows.Controls;

namespace ProjectSECURE.Views
{
    // Code-behind for the settings window
    public partial class SettingsView : Window
    {
        // Constructor initializes the theme selection ComboBox
        public SettingsView()
        {
            InitializeComponent();

            // Get the current theme from the application
            // Safely get the current theme from the application
            string currentTheme = "Light";
            if (Application.Current is App app && app.CurrentTheme != null)
            {
                currentTheme = app.CurrentTheme;
            }

            // Update ComboBox selection based on current theme, with null check
            if (ThemeComboBox != null)
            {
                ThemeComboBox.SelectedIndex = currentTheme == "Dark" ? 1 : 0;
            }
        }

        // Handles theme selection change and applies the selected theme
        private void ThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            var selectedItem = e.AddedItems[0] as ComboBoxItem;
            if (selectedItem == null) return;
            var selected = selectedItem.Content?.ToString() ?? "Claro";

            string theme = selected == "Escuro" ? "Dark" : "Light";

            if (Application.Current is App app)
            {
                app.ApplyTheme(theme);
            }
        }
    }
}
