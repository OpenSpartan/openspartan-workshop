using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;

namespace OpenSpartan.Workshop.Converters
{
    // Returns an ImageSource pointing at the converter parameter (an app-package
    // resource path) when the bound value is a non-empty string; otherwise null.
    // Used to gate showing a "marker" icon based on whether some text-bearing
    // property has been populated for the data item.
    internal sealed class StringAvailabilityToParameterSource : IValueConverter
    {
        // Cache built ImageSource instances by parameter path; same rationale as
        // ServicePathToLocalPathConverter — virtualized day templates re-evaluate
        // the binding on every scroll recycle, and re-creating the image source
        // each time causes blank cards.
        private static readonly ConcurrentDictionary<string, ImageSource> _imageSourceCache =
            new(StringComparer.OrdinalIgnoreCase);

        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str && !string.IsNullOrEmpty(str)
                && parameter is string paramPath && !string.IsNullOrEmpty(paramPath))
            {
                return _imageSourceCache.GetOrAdd(paramPath, BuildImageSource);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private static ImageSource? BuildImageSource(string paramPath)
        {
            var uri = ToAppPackageUri(paramPath);
            if (uri == null)
            {
                return null;
            }

            // Pick the right ImageSource subclass based on file extension. SVG must
            // use SvgImageSource; raster formats use BitmapImage.
            if (paramPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return new SvgImageSource(uri);
            }

            return new BitmapImage(uri);
        }

        private static Uri? ToAppPackageUri(string path)
        {
            // Bare absolute-looking paths ("/CustomImages/foo.svg") are app-package
            // relative; prefix with ms-appx:// so they resolve against the deployed app.
            if (path.StartsWith('/') || path.StartsWith('\\'))
            {
                return new Uri($"ms-appx://{path.Replace('\\', '/')}");
            }

            return Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
        }
    }
}
