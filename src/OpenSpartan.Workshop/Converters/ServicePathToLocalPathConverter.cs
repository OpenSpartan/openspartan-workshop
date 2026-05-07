using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class ServicePathToLocalPathConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string targetPath && !string.IsNullOrEmpty(targetPath))
            {
                // Normalize the targetPath by removing leading directory separators
                targetPath = targetPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Construct the local path
                var localPath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", targetPath);
                if (File.Exists(localPath))
                {
                    // Return an ImageSource directly — the binding target is Image.Source,
                    // and WinUI 3's implicit string-to-ImageSource conversion does not
                    // reliably handle absolute Windows file paths.
                    return new BitmapImage(new Uri(localPath));
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
