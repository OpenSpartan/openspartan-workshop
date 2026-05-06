using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class PerformanceToColorConverter : IValueConverter
    {
        // Lazy-initialized cached brushes - must be created on UI thread, not at static initialization
        private static SolidColorBrush? _outperformedBrush;
        private static SolidColorBrush? _underperformedBrush;
        private static SolidColorBrush? _metExpectationsBrush;
        private static SolidColorBrush? _defaultBrush;

        private static SolidColorBrush OutperformedBrush =>
            _outperformedBrush ??= new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 199, 111));
        private static SolidColorBrush UnderperformedBrush =>
            _underperformedBrush ??= new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 84, 85));
        private static SolidColorBrush MetExpectationsBrush =>
            _metExpectationsBrush ??= new SolidColorBrush(Windows.UI.Color.FromArgb(255, 115, 103, 240));
        private static SolidColorBrush DefaultBrush =>
            _defaultBrush ??= new SolidColorBrush(Colors.Black);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PerformanceMeasure performance)
            {
                return performance switch
                {
                    PerformanceMeasure.Outperformed => OutperformedBrush,
                    PerformanceMeasure.Underperformed => UnderperformedBrush,
                    PerformanceMeasure.MetExpectations => MetExpectationsBrush,
                    _ => DefaultBrush,
                };
            }

            // Return a default SolidColorBrush (e.g., Black) if the input value is null or not of expected type
            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
