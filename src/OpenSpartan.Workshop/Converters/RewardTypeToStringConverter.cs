using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class RewardTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ItemMetadataContainer type)
            {
                switch (type.Type)
                {
                    case ItemClass.XPGrant:
                        return "XP Grant";
                    case ItemClass.SpartanPoints:
                        return "Spartan Points";
                    case ItemClass.Credits:
                        return "Credits";
                    case ItemClass.XPBoost:
                        return "XP Boost";
                    case ItemClass.ChallengeReroll:
                        return "Challenge Swap";
                    case ItemClass.StandardReward:
                    default:
                        return type.ItemDetails.CommonData.Title.Value;
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
