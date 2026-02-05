using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;

namespace DestinyGhostAssistant.Utils
{
    public class MarkdownToFlowDocumentConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string markdown)
            {
                return MarkdownConverter.ToFlowDocument(markdown);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
