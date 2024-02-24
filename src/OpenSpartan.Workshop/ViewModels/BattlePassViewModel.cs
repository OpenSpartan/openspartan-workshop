using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class BattlePassViewModel : Observable
    {
        private MetadataLoadingState _battlePassLoadingState;
        private string _battlePassLoadingParameter;
        private string _currentlySelectedBattlePass;

        public static BattlePassViewModel Instance { get; } = new BattlePassViewModel();

        private BattlePassViewModel()
        {
            BattlePasses = [];
        }

        private ObservableCollection<OperationCompoundModel> _battlePasses;

        public ObservableCollection<OperationCompoundModel> BattlePasses
        {
            get => _battlePasses;
            set
            {
                if (_battlePasses != value)
                {
                    _battlePasses = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string CurrentlySelectedBattlepass
        {
            get => _currentlySelectedBattlePass;
            set
            {
                if (_currentlySelectedBattlePass != value)
                {
                    _currentlySelectedBattlePass = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string BattlePassLoadingString
        {
            get
            {
                return BattlePassLoadingState switch
                {
                    MetadataLoadingState.Loading => $"Loading battle pass details. Currently processing {BattlePassLoadingParameter}...",
                    MetadataLoadingState.Calculating => "NOOP - Never Seen",
                    MetadataLoadingState.Completed => "NOOP - Never Seen",
                    _ => "NOOP - Never Seen",
                };
            }
        }

        public MetadataLoadingState BattlePassLoadingState
        {
            get => _battlePassLoadingState;
            set
            {
                if (_battlePassLoadingState != value)
                {
                    _battlePassLoadingState = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(BattlePassLoadingString));
                }
            }
        }

        public string BattlePassLoadingParameter
        {
            get => _battlePassLoadingParameter;
            set
            {
                if (_battlePassLoadingParameter != value)
                {
                    _battlePassLoadingParameter = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(BattlePassLoadingString));
                }
            }
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
