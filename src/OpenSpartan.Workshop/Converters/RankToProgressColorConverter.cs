using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace OpenSpartan.Workshop.Converters
{
    class RankToProgressColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int rank)
            {
                if (rank == 272)
                {
                    return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 225, 75));
                }
                else
                {
                    return (SolidColorBrush)Application.Current.Resources["ProgressRingForegroundThemeBrush"];
                };
            }

            // Return a default SolidColorBrush (e.g., Black) if the input value is null or not of expected type
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
