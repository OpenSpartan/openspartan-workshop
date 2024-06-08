using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class CsrToTextRankConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Csr csr = (Csr)value;

            // Tier is zero-indexed, but the images are starting from 1, so we need to ensure
            // that we increment the tier to get the right image.
            if (!string.IsNullOrEmpty(csr.Tier))
            {
                return $"{csr.Tier} {csr.SubTier + 1}";
            }
            else
            {
                return "Unranked";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
