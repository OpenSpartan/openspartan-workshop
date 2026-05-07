using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenSpartan.Workshop.Core;
using System;
using System.Collections.Concurrent;

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
            if (value is not string targetPath || string.IsNullOrEmpty(targetPath))
            {
                return null;
            }

            // ResolveIfExists handles the leading-separator normalization, the
            // disk-existence check, and caches the positive disk stat so repeated
            // binding evaluations don't keep hitting the filesystem.
            var localPath = ImageCachePath.ResolveIfExists(targetPath);
            if (localPath == null)
            {
                return null;
            }

            return _bitmapCache.GetOrAdd(localPath, p => new BitmapImage(new Uri(p)));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
