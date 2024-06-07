using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal class CsrToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Csr csr = (Csr)value;
            string tierPath = string.Empty;
            // Tier is zero-indexed, but the images are starting from 1, so we need to ensure
            // that we increment the tier to get the right image.
            if (!string.IsNullOrEmpty(csr.Tier))
            {
                tierPath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "csr", $"{csr.Tier.ToLowerInvariant()}_{csr.SubTier + 1}.png");
            }
            else
            {
                tierPath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "csr", $"unranked_{csr.InitialMeasurementMatches - csr.MeasurementMatchesRemaining}.png");
            }
            return tierPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
