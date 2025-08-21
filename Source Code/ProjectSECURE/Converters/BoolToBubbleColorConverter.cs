using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Media;

namespace ProjectSECURE.Converters
{
    // Converts a boolean value and current theme to a chat bubble color
    public class BoolToBubbleColorConverter : IValueConverter
    {
        // Reads the current theme from the config file
        private string GetCurrentTheme()
        {
            const string configFile = "userconfig.json";

            if (!File.Exists(configFile))
                return "Light";

            try
            {
                var json = File.ReadAllText(configFile);
                var config = JsonSerializer.Deserialize<UserConfig>(json);
                return config?.UserThemePreference ?? "Light";
            }
            catch
            {
                return "Light";
            }
        }

        // Returns a Brush color for the chat bubble based on sender and theme
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSentByUser = value is bool b && b;
            string theme = GetCurrentTheme();

            Brush? brush = null;
            if (isSentByUser)
            {
                // Sent by user: blue (dark/light)
                brush = theme == "Dark"
                    ? new BrushConverter().ConvertFrom("#FF4B79A1") as Brush
                    : new BrushConverter().ConvertFrom("#ADD8E6") as Brush;
            }
            else
            {
                // Received: gray (dark/light)
                brush = theme == "Dark"
                    ? new BrushConverter().ConvertFrom("#FF2E2E2E") as Brush
                    : new BrushConverter().ConvertFrom("#F0F0F0") as Brush;
            }
            // Fallback to LightGray if conversion fails
            return brush ?? Brushes.LightGray;
        }

        // Not implemented (one-way binding only)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        // Model for reading theme preference from config
        private class UserConfig
        {
            public string? UserThemePreference { get; set; }
        }
    }
}