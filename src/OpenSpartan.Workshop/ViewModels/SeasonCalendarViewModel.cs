using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal class SeasonCalendarViewModel : Observable
    {
        private MetadataLoadingState _calendarLoadingState;
        private ObservableCollection<SeasonCalendarViewDayItem> _seasonDays;

        public static SeasonCalendarViewModel Instance { get; } = new SeasonCalendarViewModel();

        private SeasonCalendarViewModel()
        {
            SeasonDays = [];
        }

        public MetadataLoadingState CalendarLoadingState
        {
            get => _calendarLoadingState;
            set
            {
                if (_calendarLoadingState != value)
                {
                    _calendarLoadingState = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ObservableCollection<SeasonCalendarViewDayItem> SeasonDays
        {
            get => _seasonDays;
            set
            {
                if (_seasonDays != value)
                {
                    _seasonDays = value;
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
