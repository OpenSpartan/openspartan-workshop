using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal sealed class CsrProgressStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MatchTableEntity entity = value as MatchTableEntity;

            if (entity != null)
            {
                bool inverse = parameter?.ToString() == "inverse";

                return (entity.PostMatchCsr > entity.PreMatchCsr ^ inverse) ? entity.PostMatchCsr : entity.PreMatchCsr;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
