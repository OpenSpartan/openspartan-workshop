using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class MedalNameIdToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                var medalPath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "medals", $"{value}.png");
                if (File.Exists(medalPath))
                {
                    return medalPath;
                }
            }

            // Return empty string instead of null - Image controls handle this better
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}