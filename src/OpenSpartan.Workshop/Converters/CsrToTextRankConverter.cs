using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class CsrToTextRankConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value is Csr csr && !string.IsNullOrEmpty(csr.Tier)
                ? $"{csr.Tier} {csr.SubTier + 1}"
                : "Unranked";

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
