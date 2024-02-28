using CommunityToolkit.WinUI;
using Den.Dev.Orion.Models.HaloInfinite;
using OpenSpartan.Workshop.Data;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class MedalMatchesViewModel : Observable
    {
        public static MedalMatchesViewModel Instance { get; } = new MedalMatchesViewModel();

        private IncrementalLoadingCollection<MedalMatchesSource, MatchTableEntity> _matchList;
        private Medal _medal;

        public MedalMatchesViewModel()
        {
            MatchList = new IncrementalLoadingCollection<MedalMatchesSource, MatchTableEntity>();
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
    }
}
