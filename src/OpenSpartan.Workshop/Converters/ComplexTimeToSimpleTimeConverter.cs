using Microsoft.UI.Xaml.Data;
using System;
using System.Linq;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class ComplexTimeToSimpleTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan interval)
            {
                var parts = new[]
                {
                    interval.Days > 0 ? $"{interval.Days}d" : null,
                    interval.Hours > 0 ? $"{interval.Hours}hr" : null,
                    interval.Minutes > 0 ? $"{interval.Minutes}min" : null,
                    interval.Seconds > 0 || interval.TotalSeconds < 60 ? $"{interval.Seconds}sec" : null
                };

                return string.Join(" ", parts.Where(part => part != null));
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
