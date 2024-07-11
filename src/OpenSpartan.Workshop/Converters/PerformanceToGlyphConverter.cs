using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using System;
using System.Collections.Generic;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class PerformanceToGlyphConverter : IValueConverter
    {
        private readonly Dictionary<PerformanceMeasure, string> performanceGlyphMap = new()
        {
            { PerformanceMeasure.Outperformed, "\xE742" },
            { PerformanceMeasure.Underperformed, "\xE741" },
            { PerformanceMeasure.MetExpectations, "\xE73F" },
        };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PerformanceMeasure performance && performanceGlyphMap.TryGetValue(performance, out string glyph))
            {
                return glyph;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
