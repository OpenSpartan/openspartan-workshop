using Den.Dev.Grunt.Models;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class ExchangeViewModel : Observable
    {
        private APIFormattedDate _expirationDate;
        private MetadataLoadingState _exchangeLoadingState;
        private ObservableCollection<ItemMetadataContainer> _exchangeItems;

        public static ExchangeViewModel Instance { get; } = new ExchangeViewModel();

        public ExchangeViewModel()
        {
            ExchangeItems = [];
        }

        public ObservableCollection<ItemMetadataContainer> ExchangeItems
        {
            get => _exchangeItems;
            set
            {
                if (_exchangeItems != value)
                {
                    _exchangeItems = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public APIFormattedDate ExpirationDate
        {
            get => _expirationDate;
            set
            {
                if (_expirationDate != value)
                {
                    _expirationDate = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public MetadataLoadingState ExchangeLoadingState
        {
            get => _exchangeLoadingState;
            set
            {
                if (_exchangeLoadingState != value)
                {
                    _exchangeLoadingState = value;
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
