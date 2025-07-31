using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.UI.Xaml.Data;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class CsrToTooltipValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Csr currentCsr = (Csr)value;
            if (currentCsr != null)
            {
                // When the Value is -1 that means the user is not ranked - there is no
                // progress to report on.
                if (currentCsr.Value > -1)
                {
                    return $"{currentCsr.Value}/{currentCsr.NextTierStart} ({(((double)currentCsr.Value - (double)currentCsr.TierStart) / ((double)currentCsr.NextTierStart - (double)currentCsr.TierStart)) * 100.0:0.00}%)";
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
