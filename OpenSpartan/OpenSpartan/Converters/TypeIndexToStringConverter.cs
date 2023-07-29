using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Converters
{
    internal class TypeIndexToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int typeIndex = System.Convert.ToInt32(value);
            switch (typeIndex)
            {
                case 0:
                    return "Spree";
                case 1:
                    return "Mode";
                case 2:
                    return "Multikill";
                case 3:
                    return "Proficiency";
                case 4:
                    return "Skill";
                case 5:
                    return "Style";
                default:
                    return "N/A";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
