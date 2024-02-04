using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace OpenSpartan.Workshop.Converters
{
    internal class MedalNameIdToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var medalPath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "medals", $"{value.ToString()}.png");
            return medalPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}