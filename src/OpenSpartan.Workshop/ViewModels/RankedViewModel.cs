using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class RankedViewModel : Observable
    {
        private MetadataLoadingState _rankedLoadingState;
        private ObservableCollection<PlaylistCSRSnapshot> _playlists;

        public static RankedViewModel Instance { get; } = new RankedViewModel();

        public RankedViewModel()
        {
            Playlists = [];
        }

        public ObservableCollection<PlaylistCSRSnapshot> Playlists
        {
            get => _playlists;
            set
            {
                if (_playlists != value)
                {
                    _playlists = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public MetadataLoadingState RankedLoadingState
        {
            get => _rankedLoadingState;
            set
            {
                if (_rankedLoadingState != value)
                {
                    _rankedLoadingState = value;
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
