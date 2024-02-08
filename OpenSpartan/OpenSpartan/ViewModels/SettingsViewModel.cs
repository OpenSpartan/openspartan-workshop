using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
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

        public WorkshopSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings != value)
                {
                    _settings = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Version
        {
            get => $"{Configuration.Version}-{Configuration.BuildId}";
        }

        private WorkshopSettings _settings;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == nameof(Settings))
            {
                SettingsManager.StoreSettings(Settings);
            }
            OnPropertyChanged(propertyName);
        }
    }
}
