using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal class CsrToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not Csr csr)
                return string.Empty;

            string fileName = !string.IsNullOrEmpty(csr.Tier)
                ? $"{csr.Tier.ToLowerInvariant()}_{csr.SubTier + 1}.png"
                : $"unranked_{csr.InitialMeasurementMatches - csr.MeasurementMatchesRemaining}.png";

            return Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "csr", fileName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
