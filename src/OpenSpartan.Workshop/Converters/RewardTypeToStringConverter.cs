using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class RewardTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            RewardMetaContainer type = (RewardMetaContainer)value;
            return type.Type switch
            {
                RewardType.XPGrant => "XP Grant",
                RewardType.SpartanPoints => "Spartan Points",
                RewardType.Credits => "Credits",
                RewardType.XPBoost => "XP Boost",
                RewardType.ChallengeReroll => "Challenge Swap",
                _ => type.ItemDetails.CommonData.Title.Value,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
