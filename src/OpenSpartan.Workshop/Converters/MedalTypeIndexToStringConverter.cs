using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class MedalTypeIndexToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int typeIndex = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
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

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
