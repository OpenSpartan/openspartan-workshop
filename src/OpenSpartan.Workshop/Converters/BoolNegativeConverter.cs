using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    public sealed class BoolNegativeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language) =>
            value is bool boolValue ? !boolValue : null;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            value is bool boolValue ? !boolValue : false;
    }
}
