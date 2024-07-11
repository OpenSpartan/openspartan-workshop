using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class RankIdentifierToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string rankIdentifier)
            {
                return Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "csr", $"{rankIdentifier}.png");
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
