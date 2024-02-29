using CommunityToolkit.WinUI;
using Den.Dev.Orion.Models.HaloInfinite;
using OpenSpartan.Workshop.Data;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using System;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class MedalMatchesViewModel : Observable, IDisposable
    {
        public static MedalMatchesViewModel Instance { get; } = new MedalMatchesViewModel();

        private MetadataLoadingState _matchLoadingState;
        private IncrementalLoadingCollection<MedalMatchesSource, MatchTableEntity> _matchList;
        private Medal _medal;

        public MedalMatchesViewModel()
        {
            MatchList = [];
        }

        public string MatchLoadingString
        {
            get
            {
                return MatchLoadingState switch
                {
                    MetadataLoadingState.Calculating => $"NOOP - Never Seen",
                    MetadataLoadingState.Loading => $"Querying matches from database...",
                    MetadataLoadingState.Completed => "Completed",
                    _ => "NOOP - Never Seen",
                };
            }
        }

        public MetadataLoadingState MatchLoadingState
        {
            get => _matchLoadingState;
            set
            {
                if (_matchLoadingState != value)
                {
                    _matchLoadingState = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(MatchLoadingString));
                }
            }
        }

        public Medal Medal
        { 
            get => _medal;
            set 
            {
                if (_medal != value)
                {
                    _medal = value;
                    NotifyPropertyChanged();
                }
            } 
        }
        public IncrementalLoadingCollection<MedalMatchesSource, MatchTableEntity> MatchList
        {
            get => _matchList;
            set
            {
                if (_matchList != value)
                {
                    _matchList = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupManagedResources();
            }
        }

        private void CleanupManagedResources()
        { 
            this.MatchList.Clear();
            this.MatchList = null;
            this.Medal = null;
        }

        ~MedalMatchesViewModel()
        {
            Dispose(false);
        }
    }
}
