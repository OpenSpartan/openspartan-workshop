using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using OpenSpartan.Models;
using System;

namespace OpenSpartan.Converters
{
    internal class MatchLoadingStateToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var state = (MatchLoadingState)value;
            
            if (state != MatchLoadingState.Completed)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
