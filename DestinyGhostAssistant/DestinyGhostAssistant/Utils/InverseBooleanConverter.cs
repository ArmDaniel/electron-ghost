using System;
using System.Globalization;
using System.Windows.Data;

namespace DestinyGhostAssistant.Utils
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return Binding.DoNothing; // Or throw an exception, depending on desired behavior
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return Binding.DoNothing; // Or throw an exception
        }
    }
}
