using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class MedalDifficultyToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int typeIndex)
            {
                GradientStopCollection gCollection = [];

                switch (typeIndex)
                {
                    // Normal
                    case 0:
                        gCollection.Add(new GradientStop() { Color = Color.FromArgb(255, 64, 64, 64), Offset = 0.3 });
                        gCollection.Add(new GradientStop() { Color = Color.FromArgb(255, 32, 91, 34), Offset = 0.7 });
                        break;
                    // Heroic
                    case 1:
                        gCollection.Add(new GradientStop() { Color = Color.FromArgb(255, 64, 64, 64), Offset = 0.3 });
                        gCollection.Add(new GradientStop() { Color = Color.FromArgb(255, 32, 50, 79), Offset = 0.7 });
                        break;
                    // Legendary
                    case 2:
                        gCollection.Add(new GradientStop() { Color = Color.FromArgb(255, 64, 64, 64), Offset = 0.3 });
                        gCollection.Add(new GradientStop() { Color = Color.FromArgb(255, 71, 36, 116), Offset = 0.7 });
                        break;
                    // Mythic
                    case 3:
                        gCollection.Add(new GradientStop() { Color = Color.FromArgb(255, 64, 64, 64), Offset = 0.3 });
                        gCollection.Add(new GradientStop() { Color = Color.FromArgb(255, 92, 31, 40), Offset = 0.7 });
                        break;
                    default:
                        return "N/A";
                }

                return new LinearGradientBrush(gCollection, 90);
            }

            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
