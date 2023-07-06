using OpenSpartan.Shared;
using System.Runtime.CompilerServices;

namespace OpenSpartan.ViewModels
{
    internal class MatchesViewModel : Observable
    {
        public static MatchesViewModel Instance { get; } = new MatchesViewModel();

        private MatchesViewModel() { }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
