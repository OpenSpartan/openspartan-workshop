using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Core;
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
                return ImageCachePath.ResolveIfExists(Path.Combine("csr", $"{rankIdentifier}.png")) ?? string.Empty;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
