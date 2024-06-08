using Den.Dev.Orion.Models.HaloInfinite;
using OpenSpartan.Workshop.Core;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal sealed class HomeViewModel : Observable
    {
        public static HomeViewModel Instance { get; } = new HomeViewModel();

        private HomeViewModel() { }

        private string _gamerTag;
        private string _xuid;
        private PlayerServiceRecord _serviceRecord;
        private RewardTrackResultContainer _careerSnapshot;
        private string _title;
        private string _rankImage;
        private string _adornmentImage;
        private string _serviceTag;
        private string _nameplate;
        private string _emblem;
        private string _backdrop;
        private string _idBadgeTextColor;
        private int? _currentRankExperience;
        private int? _requiredRankExperience;
        private int? _maxRank;
        private string _seasonalBackground;
        private int? _experienceEarnedToDate;
        private int? _experienceTotalRequired;

        public string Gamertag
        { 
            get => _gamerTag;
            set
            {
                if (_gamerTag != value)
                {
                    _gamerTag = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Xuid
        {
            get => _xuid;
            set
            {
                if (_xuid != value)
                {
                    _xuid = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public PlayerServiceRecord ServiceRecord
        {
            get => _serviceRecord;
            set
            {
                if (_serviceRecord != value)
                {
                    _serviceRecord = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public RewardTrackResultContainer CareerSnapshot
        {
            get => _careerSnapshot;
            set
            {
                if (_careerSnapshot != value)
                {
                    _careerSnapshot = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string RankImage
        {
            get => _rankImage;
            set
            {
                if (_rankImage != value)
                {
                    _rankImage = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string AdornmentImage
        {
            get => _adornmentImage;
            set
            {
                if (_adornmentImage != value)
                {
                    _adornmentImage = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string ServiceTag
        {
            get => _serviceTag;
            set
            {
                if (_serviceTag != value)
                {
                    _serviceTag = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Nameplate
        {
            get => _nameplate;
            set
            {
                if (_nameplate != value)
                {
                    _nameplate = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Emblem
        {
            get => _emblem;
            set
            {
                if (_emblem != value)
                {
                    _emblem = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Backdrop
        {
            get => _backdrop;
            set
            {
                if (_backdrop != value)
                {
                    _backdrop = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string IDBadgeTextColor
        {
            get => _idBadgeTextColor;
            set
            {
                if (_idBadgeTextColor != value)
                {
                    _idBadgeTextColor = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int? CurrentRankExperience
        {
            get => _currentRankExperience;
            set
            {
                if (_currentRankExperience != value)
                {
                    _currentRankExperience = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(RankProgress));
                }
            }
        }

        public int? RequiredRankExperience
        {
            get => _requiredRankExperience;
            set
            {
                if (_requiredRankExperience != value)
                {
                    _requiredRankExperience = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(RankProgress));
                }
            }
        }

        public double? RankProgress
        {
            get 
            {
                if (CurrentRankExperience != null && RequiredRankExperience != null)
                {
                    return Convert.ToDouble(CurrentRankExperience) / Convert.ToDouble(RequiredRankExperience);
                }
                else
                {
                    return 0;
                }
            }
        }

        public int? MaxRank
        {
            get => _maxRank;
            set
            {
                if (_maxRank != value)
                {
                    _maxRank = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string SeasonalBackground
        {
            get => _seasonalBackground;
            set
            {
                if (_seasonalBackground != value)
                {
                    _seasonalBackground = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int? ExperienceEarnedToDate
        {
            get => _experienceEarnedToDate;
            set
            {
                if (_experienceEarnedToDate != value)
                {
                    _experienceEarnedToDate = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(ExperienceRemaining));
                    NotifyPropertyChanged(nameof(ExperienceProgress));
                }
            }
        }

        public int? ExperienceTotalRequired
        {
            get => _experienceTotalRequired;
            set
            {
                if (_experienceTotalRequired != value)
                {
                    _experienceTotalRequired = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(ExperienceRemaining));
                    NotifyPropertyChanged(nameof(ExperienceProgress));
                }
            }
        }

        public int? ExperienceRemaining
        {
            get => ExperienceTotalRequired - ExperienceEarnedToDate;
        }

        public double? ExperienceProgress
        {
            get => Convert.ToDouble(ExperienceEarnedToDate, CultureInfo.InvariantCulture) / Convert.ToDouble(ExperienceTotalRequired, CultureInfo.InvariantCulture);
        }

        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
