using OpenSpartan.Models;
using OpenSpartan.Shared;
using System.Runtime.CompilerServices;

namespace OpenSpartan.ViewModels
{
    internal class MatchesViewModel : Observable
    {
        private MatchState _matchState;

        public static MatchesViewModel Instance { get; } = new MatchesViewModel();

        private MatchesViewModel() { }

        public MatchState MatchState
        {
            get => _matchState;
            set
            {
                if (_matchState != value)
                {
                    _matchState = value;
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
