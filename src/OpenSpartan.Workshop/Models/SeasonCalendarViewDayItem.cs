using Microsoft.UI.Xaml.Media;
using OpenSpartan.Workshop.Core;
using System;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.Models
{
    internal sealed class SeasonCalendarViewDayItem: Observable
    {
        public SeasonCalendarViewDayItem()
        {
        }

        private DateTime _dateTime;
        private string _csrSeasonText = string.Empty;
        private SolidColorBrush? _csrSeasonMarkerColor;
        private string _regularSeasonText = string.Empty;
        private SolidColorBrush? _regularSeasonMarkerColor;
        private string _seasonBackgroundPath = string.Empty;
        private string _operationBackgroundPath = string.Empty;
        private string _eventBackgroundPath = string.Empty;

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

        public SolidColorBrush? CSRSeasonMarkerColor
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

        public SolidColorBrush? RegularSeasonMarkerColor
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

        // Three layered image slots so that overlapping calendar entries (a season
        // contains operations, which can be running at the same time as weekly
        // events) don't clobber each other's images. Each layer is set independently
        // by the corresponding loop in PopulateSeasonCalendar; the binding-facing
        // BackgroundImagePath picks the most specific layer that has an image.
        public string SeasonBackgroundPath
        {
            get => _seasonBackgroundPath;
            set
            {
                if (_seasonBackgroundPath != value)
                {
                    _seasonBackgroundPath = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(BackgroundImagePath));
                }
            }
        }

        public string OperationBackgroundPath
        {
            get => _operationBackgroundPath;
            set
            {
                if (_operationBackgroundPath != value)
                {
                    _operationBackgroundPath = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(BackgroundImagePath));
                }
            }
        }

        public string EventBackgroundPath
        {
            get => _eventBackgroundPath;
            set
            {
                if (_eventBackgroundPath != value)
                {
                    _eventBackgroundPath = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(BackgroundImagePath));
                }
            }
        }

        // Priority: operation > event > season. Operation imagery is the most
        // distinctive marker for a calendar day; events and seasons fall back
        // when a more specific layer isn't populated.
        public string BackgroundImagePath =>
            !string.IsNullOrEmpty(_operationBackgroundPath) ? _operationBackgroundPath
            : !string.IsNullOrEmpty(_eventBackgroundPath) ? _eventBackgroundPath
            : !string.IsNullOrEmpty(_seasonBackgroundPath) ? _seasonBackgroundPath
            : string.Empty;

        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
