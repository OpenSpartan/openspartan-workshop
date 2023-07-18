using OpenSpartan.Models;
using OpenSpartan.Shared;
using System.Runtime.CompilerServices;

namespace OpenSpartan.ViewModels
{
    internal class MatchesViewModel : Observable
    {
        private MatchLoadingState _matchLoadingState;
        private string _matchLoadingParameter;

        public static MatchesViewModel Instance { get; } = new MatchesViewModel();

        private MatchesViewModel()
        {
            MatchLoadingParameter = "0";
        }

        public string MatchLoadingString
        {
            get
            {
                switch (MatchLoadingState)
                {
                    case MatchLoadingState.Calculating:
                        return $"Calculating matches. Identified {MatchLoadingParameter} matches so far...";
                    case MatchLoadingState.Loading:
                        return $"Loading match details. Currently processing {MatchLoadingParameter}...";
                    default:
                        return "NOOP - Never Seen";
                }
            }
        }

        public MatchLoadingState MatchLoadingState
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

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
