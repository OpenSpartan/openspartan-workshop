using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class CommaAfterThousandsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return String.Format("{0:n0}", value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
