using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DestinyGhostAssistant.Models; // For MessageSender

namespace DestinyGhostAssistant.Utils
{
    public class AssistantMessageVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageSender sender && sender == MessageSender.Assistant)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("This converter is one-way.");
        }
    }
}
