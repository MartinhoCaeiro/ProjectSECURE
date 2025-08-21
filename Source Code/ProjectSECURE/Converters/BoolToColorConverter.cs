using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProjectSECURE.Converters
{
    // Converts a boolean value to a status color (green/red/gray)
    public class BoolToColorConverter : IValueConverter
    {
        // Returns green if true, red if false, gray if not a bool
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        // Not implemented (one-way binding only)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
