using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Models;
using System;

namespace OpenSpartan.Workshop.Converters
{
    internal class RewardTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            RewardType type = (RewardType)value;
            return type switch
            {
                RewardType.XPGrant or
                RewardType.SpartanPoints or
                RewardType.Credits or
                RewardType.XPBoost or
                RewardType.ChallengeReroll => Visibility.Visible,
                _ => Visibility.Collapsed,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
