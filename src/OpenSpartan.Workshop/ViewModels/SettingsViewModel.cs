﻿using OpenSpartan.Workshop.Core;
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
