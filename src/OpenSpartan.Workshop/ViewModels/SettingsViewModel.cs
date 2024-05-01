using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal sealed class SettingsViewModel : Observable
    {
        public static SettingsViewModel Instance { get; } = new SettingsViewModel();

        public static string Gamertag => HomeViewModel.Instance.Gamertag;

        public static string Xuid => HomeViewModel.Instance.Xuid;

        public bool SyncSettings
        {
            get
            {
                return Settings.SyncSettings;
            }
            set
            {
                if (Settings.SyncSettings != value)
                {
                    Settings.SyncSettings = value;
                    SettingsManager.StoreSettings(Settings);
                    NotifyPropertyChanged();
                }
            }
        }

        public string Release
        {
            get
            {
                return Settings.Release;
            }
            set
            {
                if (Settings.Release != value)
                {
                    Settings.Release = value;
                    SettingsManager.StoreSettings(Settings);
                    NotifyPropertyChanged();
                }
            }
        }

        public bool EnableLogging
        {
            get
            {
                return Settings.EnableLogging;
            }
            set
            {
                if (Settings.EnableLogging != value)
                {
                    Settings.EnableLogging = value;
                    SettingsManager.StoreSettings(Settings);
                    NotifyPropertyChanged();
                }
            }
        }

        public bool UseBroker
        {
            get
            { 
                return Settings.UseBroker;
            }
            set
            {
                if (Settings.UseBroker != value)
                {
                    Settings.UseBroker = value;
                    SettingsManager.StoreSettings(Settings);
                    NotifyPropertyChanged();
                }
            }
        }

        public string Sandbox
        {
            get
            {
                return Settings.Sandbox;
            }
            set
            {
                if (Settings.Sandbox != value)
                {
                    Settings.Sandbox = value;
                    SettingsManager.StoreSettings(Settings);
                    NotifyPropertyChanged();
                }
            }
        }

        public string Build
        {
            get
            {
                return Settings.Build;
            }
            set
            {
                if (Settings.Build != value)
                {
                    Settings.Build = value;
                    SettingsManager.StoreSettings(Settings);
                    NotifyPropertyChanged();
                }
            }
        }

        public bool UseObanClearance
        {
            get
            {
                return Settings.UseObanClearance;
            }
            set
            {
                if (Settings.UseObanClearance != value)
                {
                    Settings.UseObanClearance = value;
                    SettingsManager.StoreSettings(Settings);
                    NotifyPropertyChanged();
                }
            }
        }

        public WorkshopSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings != value)
                {
                    _settings = value;
                    SettingsManager.StoreSettings(Settings);
                    NotifyPropertyChanged();
                }
            }
        }

        public static string Version
        {
            get => $"{Configuration.Version}-{Configuration.BuildId}";
        }

        private WorkshopSettings _settings;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
