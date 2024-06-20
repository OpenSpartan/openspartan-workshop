using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class StringAvailabilityToParameterSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Check if value is a non-null and non-empty string
            if (value is string str && !String.IsNullOrEmpty(str))
            {
                // Return the converter parameter as string
                return parameter?.ToString() ?? string.Empty;
            }

            // Return null or empty string if value is null or empty
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // ConvertBack is not needed for this example
        }
    }
}
