using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class MedalDifficultyToBrushConverter : IValueConverter
    {
        // Lazy-initialized cached gradient brushes - must be created on UI thread, not at static initialization
        private static LinearGradientBrush? _normalBrush;
        private static LinearGradientBrush? _heroicBrush;
        private static LinearGradientBrush? _legendaryBrush;
        private static LinearGradientBrush? _mythicBrush;

        private static LinearGradientBrush NormalBrush =>
            _normalBrush ??= CreateGradientBrush(Color.FromArgb(255, 32, 91, 34));
        private static LinearGradientBrush HeroicBrush =>
            _heroicBrush ??= CreateGradientBrush(Color.FromArgb(255, 32, 50, 79));
        private static LinearGradientBrush LegendaryBrush =>
            _legendaryBrush ??= CreateGradientBrush(Color.FromArgb(255, 71, 36, 116));
        private static LinearGradientBrush MythicBrush =>
            _mythicBrush ??= CreateGradientBrush(Color.FromArgb(255, 92, 31, 40));

        private static LinearGradientBrush CreateGradientBrush(Color endColor)
        {
            var gCollection = new GradientStopCollection
            {
                new GradientStop() { Color = Color.FromArgb(255, 64, 64, 64), Offset = 0.3 },
                new GradientStop() { Color = endColor, Offset = 0.7 }
            };
            return new LinearGradientBrush(gCollection, 90);
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int typeIndex)
            {
                return typeIndex switch
                {
                    0 => NormalBrush,      // Normal
                    1 => HeroicBrush,      // Heroic
                    2 => LegendaryBrush,   // Legendary
                    3 => MythicBrush,      // Mythic
                    _ => "N/A",
                };
            }

            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
