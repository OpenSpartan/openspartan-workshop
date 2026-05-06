using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class RankIdentifierToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string rankIdentifier && !string.IsNullOrEmpty(rankIdentifier))
            {
                var imagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "csr", $"{rankIdentifier}.png");
                if (File.Exists(imagePath))
                {
                    return imagePath;
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
