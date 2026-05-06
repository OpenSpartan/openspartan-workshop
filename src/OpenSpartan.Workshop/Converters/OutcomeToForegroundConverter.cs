using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class OutcomeToForegroundConverter : IValueConverter
    {
        // Lazy-initialized cached brushes - must be created on UI thread, not at static initialization
        private static SolidColorBrush _didNotFinishBrush;
        private static SolidColorBrush _lossBrush;
        private static SolidColorBrush _tieBrush;
        private static SolidColorBrush _winBrush;
        private static SolidColorBrush _defaultBrush;

        private static SolidColorBrush DidNotFinishBrush =>
            _didNotFinishBrush ??= new SolidColorBrush(Windows.UI.Color.FromArgb(255, 115, 103, 240));
        private static SolidColorBrush LossBrush =>
            _lossBrush ??= new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 84, 85));
        private static SolidColorBrush TieBrush =>
            _tieBrush ??= new SolidColorBrush(Windows.UI.Color.FromArgb(255, 115, 103, 240));
        private static SolidColorBrush WinBrush =>
            _winBrush ??= new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 199, 111));
        private static SolidColorBrush DefaultBrush =>
            _defaultBrush ??= new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Outcome outcome)
            {
                return outcome switch
                {
                    Outcome.DidNotFinish => DidNotFinishBrush,
                    Outcome.Loss => LossBrush,
                    Outcome.Tie => TieBrush,
                    Outcome.Win => WinBrush,
                    _ => DefaultBrush,
                };
            }

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
