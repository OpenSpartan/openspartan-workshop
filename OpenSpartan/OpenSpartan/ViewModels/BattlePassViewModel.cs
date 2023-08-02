using OpenSpartan.Models;
using OpenSpartan.Shared;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace OpenSpartan.ViewModels
{
    internal class BattlePassViewModel : Observable
    {
        public static BattlePassViewModel Instance { get; } = new BattlePassViewModel();

        private BattlePassViewModel()
        {
            BattlePasses = new ObservableCollection<OperationCompoundModel>();
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

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
