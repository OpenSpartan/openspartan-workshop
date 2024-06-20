using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class ServicePathToLocalPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var localPath = string.Empty;

            if (value != null)
            {
                string? targetPath = value as string;

                if (Path.IsPathRooted(targetPath))
                {
                    targetPath = targetPath.TrimStart(Path.DirectorySeparatorChar);
                    targetPath = targetPath.TrimStart(Path.AltDirectorySeparatorChar);
                }

                localPath = Path.Join(Core.Configuration.AppDataDirectory, "imagecache", targetPath);
            }

            return localPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}