using CommunityToolkit.WinUI;
using OpenSpartan.Workshop.Data;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class MatchesViewModel : Observable
    {
        private MetadataLoadingState _matchLoadingState;
        private string _matchLoadingParameter;
        private IncrementalLoadingCollection<MatchesSource, MatchTableEntity> _matchList;

        public static MatchesViewModel Instance { get; } = new MatchesViewModel();

        private MatchesViewModel()
        {
            MatchLoadingParameter = "0";
            MatchList = new IncrementalLoadingCollection<MatchesSource, MatchTableEntity>();
        }

        public string MatchLoadingString
        {
            get
            {
                return MatchLoadingState switch
                {
                    MetadataLoadingState.Calculating => $"Calculating matches. Identified {MatchLoadingParameter} matches so far...",
                    MetadataLoadingState.Loading => $"Loading match details. Currently processing {MatchLoadingParameter}...",
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

        public string MatchLoadingParameter
        {
            get => _matchLoadingParameter;
            set
            {
                if (_matchLoadingParameter != value)
                {
                    _matchLoadingParameter = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(MatchLoadingString));
                }
            }
        }

        public IncrementalLoadingCollection<MatchesSource, MatchTableEntity> MatchList
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
    }
}
