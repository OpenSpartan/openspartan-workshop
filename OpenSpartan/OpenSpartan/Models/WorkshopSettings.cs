using OpenSpartan.Workshop.Shared;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace OpenSpartan.Workshop.Models
{
    internal class WorkshopSettings : Observable
    {
        private string _audience;
        private string _sandbox;
        private string _release;
        private string _build;
        private bool _syncSettings;
        private string _apiVersion;
        private string _headerImagePath;

        [JsonPropertyName("audience")]
        public string Audience
        {
            get => _audience;
            set
            {
                if (_audience != value)
                {
                    _audience = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("sandbox")]
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

        [JsonPropertyName("build")]
        public string Build
        {
            get => _build;
            set
            {
                if (_build != value)
                {
                    _build = value;
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
            // Store settings every time they change.
            SettingsManager.StoreSettings(this);
            OnPropertyChanged(propertyName);
        }
    }
}
