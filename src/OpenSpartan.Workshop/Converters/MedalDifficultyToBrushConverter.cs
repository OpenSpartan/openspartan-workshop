﻿using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class MedalDifficultyToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int typeIndex = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
            var gCollection = new GradientStopCollection();

            switch (typeIndex)
            {
                // Normal
                case 0:
                    gCollection = new GradientStopCollection
                    {
                        new GradientStop() { Color = Windows.UI.Color.FromArgb(255, 64, 64, 64), Offset = 0.3 },
                        new GradientStop() { Color = Windows.UI.Color.FromArgb(255, 32, 91, 34), Offset = 0.7 },
                    };

                    return new LinearGradientBrush(gCollection, 90);
                // Heroic
                case 1:
                    gCollection = new GradientStopCollection
                    {
                        new GradientStop() { Color = Windows.UI.Color.FromArgb(255, 64, 64, 64), Offset = 0.3 },
                        new GradientStop() { Color = Windows.UI.Color.FromArgb(255, 32, 50, 79), Offset = 0.7 },
                    };

                    return new LinearGradientBrush(gCollection, 90);
                // Legendary
                case 2:
                    gCollection = new GradientStopCollection
                    {
                        new GradientStop() { Color = Windows.UI.Color.FromArgb(255, 64, 64, 64), Offset = 0.3 },
                        new GradientStop() { Color = Windows.UI.Color.FromArgb(255, 71, 36, 116), Offset = 0.7 },
                    };

                    return new LinearGradientBrush(gCollection, 90);
                // Mythic
                case 3:
                    gCollection = new GradientStopCollection
                    {
                        new GradientStop() { Color = Windows.UI.Color.FromArgb(255, 64, 64, 64), Offset = 0.3 },
                        new GradientStop() { Color = Windows.UI.Color.FromArgb(255, 92, 31, 40), Offset = 0.7 },
                    };

                    return new LinearGradientBrush(gCollection, 90);
                default:
                    return "N/A";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
