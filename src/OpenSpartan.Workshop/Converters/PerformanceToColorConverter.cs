using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace OpenSpartan.Workshop.Converters
{
    class PerformanceToColorConverter : IValueConverter
    {
        private readonly Dictionary<PerformanceMeasure, SolidColorBrush> performanceBrushMap = new Dictionary<PerformanceMeasure, SolidColorBrush>
        {
            { PerformanceMeasure.Outperformed, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 199, 111)) },
            { PerformanceMeasure.Underperformed, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 84, 85)) },
            { PerformanceMeasure.MetExpectations, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 115, 103, 240)) },
        };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PerformanceMeasure performance && performanceBrushMap.TryGetValue(performance, out SolidColorBrush brush))
            {
                return brush;
            }

            // Return a default SolidColorBrush (e.g., Black) if the conversion fails
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
