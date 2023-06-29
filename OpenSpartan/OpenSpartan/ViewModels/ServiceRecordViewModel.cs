using Den.Dev.Orion.Models.HaloInfinite;
using OpenSpartan.Shared;
using System.Runtime.CompilerServices;

namespace OpenSpartan.ViewModels
{
    internal class ServiceRecordViewModel : Observable
    {
        public static ServiceRecordViewModel Instance { get; } = new ServiceRecordViewModel();

        private ServiceRecordViewModel() { }

        private string _gamerTag;
        private string _xuid;
        private PlayerServiceRecord _serviceRecord;
        private RewardTrackResultContainer _careerSnapshot;
        private string _title;
        private string _rankImage;
        private string _adornmentImage;

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

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
