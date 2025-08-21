using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProjectSECURE.Converters
{
    // Converts a boolean value to a HorizontalAlignment for chat bubbles
    public class BoolToAlignmentConverter : IValueConverter
    {
        // If true, aligns to the right (sent by user); otherwise, aligns to the left
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSentByUser && isSentByUser)
                return HorizontalAlignment.Right;
            else
                return HorizontalAlignment.Left;
        }

        // Not implemented (one-way binding only)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
