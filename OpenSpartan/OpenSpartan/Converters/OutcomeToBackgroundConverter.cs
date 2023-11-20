using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace OpenSpartan.Converters
{
    internal class OutcomeToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Outcome outcome = (Outcome)value;

            switch (outcome)
            {
                case Outcome.DidNotFinish:
                    {
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(50, 115, 103, 240));
                    }
                case Outcome.Loss:
                    {
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(50, 234, 84, 85));
                    }
                case Outcome.Tie:
                    {
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(50, 115, 103, 240));
                    }
                case Outcome.Win:
                    {
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(50, 40, 199, 111));
                    }
                default:
                    {
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(50, 0, 0, 0));
                    }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
