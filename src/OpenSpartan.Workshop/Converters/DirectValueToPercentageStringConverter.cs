using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class DirectValueToPercentageStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not IConvertible convertible)
                return "0.00%";

            // Convert directly via IConvertible so numeric inputs bypass culture-sensitive
            // string parsing — round-tripping a double through ToString() (current culture)
            // and double.Parse(InvariantCulture) inflates values 100x on cultures whose
            // decimal separator is `,` (en-ZA, fr-FR, de-DE, ...).
            try
            {
                double doubleValue = convertible.ToDouble(CultureInfo.InvariantCulture);
                return $"{doubleValue / 100.0:P02}";
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                return "0.00%";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
