using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class ISO8601ToLocalDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("MMMM d, yyyy h:mm tt", CultureInfo.CurrentCulture);
            }

            return string.Empty; // Handle null value gracefully or return an appropriate default
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
