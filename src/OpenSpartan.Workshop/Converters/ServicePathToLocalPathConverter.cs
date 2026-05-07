using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class ServicePathToLocalPathConverter : IValueConverter
    {
        // Cache resolved BitmapImage instances by local path. WinUI's CalendarView
        // virtualizes day templates, so the binding for BackgroundImagePath can be
        // re-evaluated dozens of times as the user scrolls. Without caching, we'd
        // construct (and have to re-decode) a fresh BitmapImage every time a day
        // recycles, which causes visible blank cards while the new instance loads
        // and pressures memory because the same image (e.g. an operation logo
        // shared across hundreds of days) ends up duplicated on the heap.
        // The converter is registered once at app scope, so a static dictionary
        // is the natural place for this.
        private static readonly ConcurrentDictionary<string, BitmapImage> _bitmapCache =
            new(StringComparer.OrdinalIgnoreCase);

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
                    return _bitmapCache.GetOrAdd(localPath, p => new BitmapImage(new Uri(p)));
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
