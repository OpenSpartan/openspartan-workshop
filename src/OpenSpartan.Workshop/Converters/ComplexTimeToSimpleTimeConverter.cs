using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class ComplexTimeToSimpleTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            TimeSpan interval = (TimeSpan)value;
            return string.Format("{0:D2}d {1:D2}hr {2:D2}min {3:D2}sec", interval.Days, interval.Hours, interval.Minutes, interval.Seconds);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
