using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class CsrProgressStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MatchTableEntity entity = (MatchTableEntity)value;
            if (entity.PostMatchCsr > entity.PreMatchCsr)
            {
                if (parameter.ToString() != "inverse")
                {
                    return entity.PostMatchCsr;
                }
                else
                {
                    return entity.PreMatchCsr;
                }
            }
            else
            {
                if (parameter.ToString() != "inverse")
                {
                    return entity.PreMatchCsr;
                }
                else
                {
                    return entity.PostMatchCsr;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
