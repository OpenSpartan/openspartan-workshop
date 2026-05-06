using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class CsrToProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Csr currentCsr && currentCsr.Value > -1 &&
                currentCsr.TierStart.HasValue && currentCsr.NextTierStart.HasValue)
            {
                return (double)(currentCsr.Value - currentCsr.TierStart.Value) / (currentCsr.NextTierStart.Value - currentCsr.TierStart.Value);
            }
            return (double)0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
