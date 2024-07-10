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
            if (value is ItemClass type)
            {
                return type switch
                {
                    ItemClass.XPGrant or
                    ItemClass.SpartanPoints or
                    ItemClass.Credits or
                    ItemClass.XPBoost or
                    ItemClass.ChallengeReroll => Visibility.Visible,
                    _ => Visibility.Collapsed,
                };
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
