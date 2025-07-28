using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Media;

namespace ProjectSECURE.Converters
{
    public class BoolToBubbleColorConverter : IValueConverter
    {
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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSentByUser = value is bool b && b;
            string theme = GetCurrentTheme();

            if (isSentByUser)
            {
                return theme == "Dark"
                    ? (Brush)new BrushConverter().ConvertFrom("#FF4B79A1") // azul escuro
                    : (Brush)new BrushConverter().ConvertFrom("#ADD8E6");  // azul claro
            }
            else
            {
                return theme == "Dark"
                    ? (Brush)new BrushConverter().ConvertFrom("#FF2E2E2E") // cinzento escuro
                    : (Brush)new BrushConverter().ConvertFrom("#F0F0F0");  // cinzento claro
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private class UserConfig
        {
            public string? UserThemePreference { get; set; }
        }
    }
}