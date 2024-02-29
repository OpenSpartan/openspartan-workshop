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
            { PerformanceMeasure.Outperformed, "e742" },
            { PerformanceMeasure.Underperformed, "e741" },
            { PerformanceMeasure.MetExpectations, "e73f" },
        };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PerformanceMeasure performance && performanceGlyphMap.TryGetValue(performance, out string unicodeString))
            {
                if (int.TryParse(unicodeString, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int unicodeValue))
                {
                    char glyphChar = (char)unicodeValue;
                    return glyphChar.ToString();
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
