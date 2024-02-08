using OpenSpartan.Workshop.Shared;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class SettingsViewModel : Observable
    {
        public static SettingsViewModel Instance { get; } = new SettingsViewModel();

        public string Gamertag
        {
            get => HomeViewModel.Instance.Gamertag;
        }

        public string Xuid
        {
            get => HomeViewModel.Instance.Xuid;
        }

        public bool IsSyncedWithService
        {
            get => _isSyncedWithService;
            set
            {
                if (_isSyncedWithService != value)
                {
                    _isSyncedWithService = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Sandbox
        {
            get => _sandbox;
            set
            {
                if (_sandbox != value)
                {
                    _sandbox = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string BuildId
        {
            get => _buildId;
            set
            {
                if (_buildId != value)
                {
                    _buildId = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Release
        {
            get => _release;
            set
            {
                if (_release != value)
                {
                    _release = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isSyncedWithService;
        private string _sandbox;
        private string _buildId;
        private string _release;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
