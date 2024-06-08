using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class RewardTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ItemMetadataContainer type = (ItemMetadataContainer)value;
            return type.Type switch
            {
                ItemClass.XPGrant => "XP Grant",
                ItemClass.SpartanPoints => "Spartan Points",
                ItemClass.Credits => "Credits",
                ItemClass.XPBoost => "XP Boost",
                ItemClass.ChallengeReroll => "Challenge Swap",
                _ => type.ItemDetails.CommonData.Title.Value,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
