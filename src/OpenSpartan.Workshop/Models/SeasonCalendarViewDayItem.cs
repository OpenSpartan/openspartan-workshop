using Microsoft.UI.Xaml.Media;
using OpenSpartan.Workshop.Core;
using System;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.Models
{
    public class SeasonCalendarViewDayItem: Observable
    {
        public SeasonCalendarViewDayItem()
        {
            RegularSeasonText = string.Empty;
        }

        private DateTime _dateTime;
        private string _csrSeasonText;
        private SolidColorBrush _csrSeasonMarkerColor;
        private string _regularSeasonText;
        private SolidColorBrush _regularSeasonMarkerColor;
        private string _backgroundImagePath;

        public DateTime DateTime
        {
            get => _dateTime;
            set
            {
                if (_dateTime != value)
                {
                    _dateTime = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string CSRSeasonText
        {
            get => _csrSeasonText;
            set
            {
                if (_csrSeasonText != value)
                {
                    _csrSeasonText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public SolidColorBrush CSRSeasonMarkerColor
        {
            get => _csrSeasonMarkerColor;
            set
            {
                if (_csrSeasonMarkerColor != value)
                {
                    _csrSeasonMarkerColor = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string RegularSeasonText
        {
            get => _regularSeasonText;
            set
            {
                if (_regularSeasonText != value)
                {
                    _regularSeasonText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public SolidColorBrush RegularSeasonMarkerColor
        {
            get => _regularSeasonMarkerColor;
            set
            {
                if (_regularSeasonMarkerColor != value)
                {
                    _regularSeasonMarkerColor = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set
            {
                if (_backgroundImagePath != value)
                {
                    _backgroundImagePath = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
