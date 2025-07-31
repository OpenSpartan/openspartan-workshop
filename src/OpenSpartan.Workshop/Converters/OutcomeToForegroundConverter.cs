using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class OutcomeToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Outcome outcome)
            {
                return outcome switch
                {
                    Outcome.DidNotFinish => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 115, 103, 240)),
                    Outcome.Loss => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 84, 85)),
                    Outcome.Tie => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 115, 103, 240)),
                    Outcome.Win => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 199, 111)),
                    _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
                };
            }

            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
