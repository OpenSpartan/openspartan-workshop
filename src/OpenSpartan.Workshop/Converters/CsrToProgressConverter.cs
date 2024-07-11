using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class CsrToProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value is Csr currentCsr && currentCsr.Value > -1
                ? (double)(currentCsr.Value - currentCsr.TierStart) / (currentCsr.NextTierStart - currentCsr.TierStart)
                : (double)0;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
