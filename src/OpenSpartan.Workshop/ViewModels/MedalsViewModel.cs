using Den.Dev.Orion.Models.HaloInfinite;
using OpenSpartan.Workshop.Shared;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class MedalsViewModel : Observable
    {
        public static MedalsViewModel Instance { get; } = new MedalsViewModel();

        private MedalsViewModel()
        {
            NavigateCommand = new RelayCommand<long>(NavigateToAnotherView);
        }

        private ObservableCollection<IGrouping<int, Medal>> _medals;

        public RelayCommand<long> NavigateCommand { get; }

        public event EventHandler<long> NavigationRequested;

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

        private void NavigateToAnotherView(long parameter)
        {
            NavigationRequested?.Invoke(this, parameter);
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
