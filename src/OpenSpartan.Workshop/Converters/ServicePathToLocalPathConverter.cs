using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class ServicePathToLocalPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string targetPath)
            {
                // Normalize the targetPath by removing leading directory separators
                targetPath = targetPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Construct the local path
                var localPath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", targetPath);
                return localPath;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
