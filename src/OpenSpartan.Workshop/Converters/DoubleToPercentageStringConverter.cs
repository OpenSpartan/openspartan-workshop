using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class DoubleToPercentageStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return $"{System.Convert.ToDouble(value, CultureInfo.InvariantCulture):P2}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
