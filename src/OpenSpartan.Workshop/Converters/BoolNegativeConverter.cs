using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    public class BoolNegativeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value is bool boolValue ? !boolValue : (object)null;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            value is bool boolValue ? !boolValue : (object)false;
    }
}
