﻿using OpenSpartan.Workshop.Core;
using System.Collections.Generic;
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
        private bool _useBroker;
        private string _sandbox;
        private string _build;
        private bool _useObanClearance;
        private bool _enableLooseMatchSearch;
        private List<string> _extraRitualEvents;
        private List<string> _excludedOperations;

        public WorkshopSettings()
        {
            ExcludedOperations = [];
            ExtraRitualEvents = [];
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

        [JsonPropertyName("usebroker")]
        public bool UseBroker
        {
            get => _useBroker;
            set
            {
                if (_useBroker != value)
                {
                    _useBroker = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("loosematchsearch")]
        public bool EnableLooseMatchSearch
        {
            get => _enableLooseMatchSearch;
            set
            {
                if (_enableLooseMatchSearch != value)
                {
                    _enableLooseMatchSearch = value;
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

        [JsonPropertyName("useObanClearance")]
        public bool UseObanClearance
        {
            get => _useObanClearance;
            set
            {
                if (_useObanClearance != value)
                {
                    _useObanClearance = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("extraRitualEvents")]
        public List<string> ExtraRitualEvents
        {
            get => _extraRitualEvents;
            set
            {
                if (_extraRitualEvents != value)
                {
                    _extraRitualEvents = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("excludedOperations")]
        public List<string> ExcludedOperations
        {
            get => _excludedOperations;
            set
            {
                if (_excludedOperations != value)
                {
                    _excludedOperations = value;
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
