using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace OpenSpartan.Workshop.Converters
{
    public class BoolToDisabledBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool isActive && !isActive
                ? new SolidColorBrush((Color)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorPrimary"])
                : (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["SystemControlDisabledBaseMediumLowBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
