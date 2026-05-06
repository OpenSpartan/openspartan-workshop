using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class CsrToTooltipValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Csr currentCsr)
            {
                // When the Value is -1 that means the user is not ranked - there is no
                // progress to report on.
                if (currentCsr.Value > -1 && currentCsr.TierStart.HasValue && currentCsr.NextTierStart.HasValue)
                {
                    return $"{currentCsr.Value}/{currentCsr.NextTierStart} ({(((double)currentCsr.Value - (double)currentCsr.TierStart.Value) / ((double)currentCsr.NextTierStart.Value - (double)currentCsr.TierStart.Value)) * 100.0:0.00}%)";
                }
                else
                {
                    return "Unranked";
                }
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
