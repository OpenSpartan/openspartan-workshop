using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class CommaAfterThousandsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            string.Format(CultureInfo.InvariantCulture, "{0:n0}", value);

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
