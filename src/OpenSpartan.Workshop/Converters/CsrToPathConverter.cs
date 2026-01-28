using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class CsrToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not Csr csr)
                return string.Empty;

            string fileName;
            if (!string.IsNullOrEmpty(csr.Tier) && csr.SubTier.HasValue)
            {
                fileName = $"{csr.Tier.ToLowerInvariant()}_{csr.SubTier + 1}.png";
            }
            else if (csr.InitialMeasurementMatches.HasValue && csr.MeasurementMatchesRemaining.HasValue)
            {
                fileName = $"unranked_{csr.InitialMeasurementMatches - csr.MeasurementMatchesRemaining}.png";
            }
            else
            {
                return string.Empty;
            }

            var imagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "csr", fileName);
            if (System.IO.File.Exists(imagePath))
            {
                return imagePath;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
