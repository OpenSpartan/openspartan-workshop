using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Core;
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
#pragma warning disable CA1308 // CSR rank tiers are stored as lowercase file names ("bronze_1.png"); ToLowerInvariant matches the on-disk convention.
                fileName = $"{csr.Tier.ToLowerInvariant()}_{csr.SubTier + 1}.png";
#pragma warning restore CA1308
            }
            else if (csr.InitialMeasurementMatches.HasValue && csr.MeasurementMatchesRemaining.HasValue)
            {
                fileName = $"unranked_{csr.InitialMeasurementMatches - csr.MeasurementMatchesRemaining}.png";
            }
            else
            {
                return string.Empty;
            }

            return ImageCachePath.ResolveIfExists(Path.Combine("csr", fileName)) ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
