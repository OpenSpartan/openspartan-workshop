using OpenSpartan.Workshop.Core;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace OpenSpartan.Workshop.Models
{
    internal sealed class WorkshopSettings : Observable
    {
        private string _release;
        private bool _syncSettings;
        private bool _enableLogging;
        private string _apiVersion;
        private string _headerImagePath;

        [JsonPropertyName("release")]
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

        [JsonPropertyName("syncsettings")]
        public bool SyncSettings
        {
            get => _syncSettings;
            set
            {
                if (_syncSettings != value)
                {
                    _syncSettings = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("enablelogging")]
        public bool EnableLogging
        {
            get => _enableLogging;
            set
            {
                if (_enableLogging != value)
                {
                    _enableLogging = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("apiversion")]
        public string APIVersion
        {
            get => _apiVersion;
            set
            {
                if (_apiVersion != value)
                {
                    _apiVersion = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("headerimagepath")]
        public string HeaderImagePath
        {
            get => _headerImagePath;
            set
            {
                if (_headerImagePath != value)
                {
                    _headerImagePath = value;
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
