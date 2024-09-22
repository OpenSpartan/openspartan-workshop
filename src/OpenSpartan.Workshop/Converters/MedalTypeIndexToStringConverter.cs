using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class MedalTypeIndexToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int typeIndex)
            {
                return typeIndex switch
                {
                    0 => "Spree",
                    1 => "Mode",
                    2 => "Multikill",
                    3 => "Proficiency",
                    4 => "Skill",
                    5 => "Style",
                    _ => "N/A",
                };
            }

            return "N/A"; // Handle non-integer or null values gracefully
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
