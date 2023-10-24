using Microsoft.UI.Xaml.Data;
using System;
using System.IO;

namespace OpenSpartan.Converters
{
    internal class ServicePathToLocalPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var medalPath = string.Empty;

            if (value != null)
            {
                medalPath = Path.Join(Core.Configuration.AppDataDirectory, "imagecache", value.ToString());
            }

            return medalPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}