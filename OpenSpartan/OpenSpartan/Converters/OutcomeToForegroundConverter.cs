using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace OpenSpartan.Converters
{
    internal class OutcomeToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Outcome outcome = (Outcome)value;

            switch (outcome)
            {
                case Outcome.DidNotFinish:
                    {
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 222, 226, 230));
                    }
                default:
                    {
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
                    }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
