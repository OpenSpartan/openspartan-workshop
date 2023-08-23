using Den.Dev.Orion.Models.HaloInfinite;
using OpenSpartan.Shared;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenSpartan.ViewModels
{
    internal class MedalsViewModel : Observable
    {
        public static MedalsViewModel Instance { get; } = new MedalsViewModel();

        private MedalsViewModel() { }

        private ObservableCollection<IGrouping<int, Medal>> _medals;

        public ObservableCollection<IGrouping<int, Medal>> Medals
        {
            get => _medals;
            set
            {
                if (_medals != value)
                {
                    _medals = value;
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
