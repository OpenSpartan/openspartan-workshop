using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSpartan.Workshop.Converters
{
    using Microsoft.UI.Xaml.Data;
    using Microsoft.UI.Xaml.Media;
    using Windows.UI;

    public class BoolToDisabledBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isActive && !isActive)
            {
                // Return a brush for an active state
                return new SolidColorBrush((Color)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorPrimary"]);
            }
            else
            {
                // Return the system-defined disabled brush
                return (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["SystemControlDisabledBaseMediumLowBrush"];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
