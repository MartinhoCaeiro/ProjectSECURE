using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChatAppWPF.Converters
{
    public class BoolToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSentByUser && isSentByUser)
                return HorizontalAlignment.Right;
            else
                return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
