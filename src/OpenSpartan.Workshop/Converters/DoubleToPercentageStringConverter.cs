using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class DoubleToPercentageStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not IConvertible convertible)
                return "0.00%";

            // See DirectValueToPercentageStringConverter for the culture rationale: avoid
            // ToString() -> double.Parse(InvariantCulture) round-trips that mistake the
            // current-culture decimal `,` for an invariant-culture thousands separator.
            try
            {
                double doubleValue = convertible.ToDouble(CultureInfo.InvariantCulture);
                return $"{doubleValue:P2}";
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
