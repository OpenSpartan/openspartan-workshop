using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Core;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class MedalNameIdToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return ImageCachePath.ResolveIfExists(Path.Combine("medals", $"{value}.png")) ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
