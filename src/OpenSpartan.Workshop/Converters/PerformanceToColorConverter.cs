using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class PerformanceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PerformanceMeasure performance)
            {
                return performance switch
                {
                    PerformanceMeasure.Outperformed => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 199, 111)),
                    PerformanceMeasure.Underperformed => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 84, 85)),
                    PerformanceMeasure.MetExpectations => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 115, 103, 240)),
                    _ => new SolidColorBrush(Colors.Black),
                };
            }

            // Return a default SolidColorBrush (e.g., Black) if the input value is null or not of expected type
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
