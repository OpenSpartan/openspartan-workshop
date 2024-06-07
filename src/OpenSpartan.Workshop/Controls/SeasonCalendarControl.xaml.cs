using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenSpartan.Workshop.Controls
{

    public sealed partial class SeasonCalendarControl : UserControl
    {
        public static readonly DependencyProperty DayItemsProperty = DependencyProperty.Register(
            nameof(DayItems),
            typeof(IEnumerable<SeasonCalendarViewDayItem>),
            typeof(SeasonCalendarControl),
            new PropertyMetadata(default));

        public SeasonCalendarControl()
        {
            InitializeComponent();
            this.CalendarViewControl.CalendarViewDayItemChanging += CalendarViewControl_CalendarViewDayItemChanging;
        }

        public IEnumerable<SeasonCalendarViewDayItem> DayItems
        {
            get => (IEnumerable<SeasonCalendarViewDayItem>)GetValue(DayItemsProperty);
            set => SetValue(DayItemsProperty, value);
        }

        private void CalendarViewControl_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            if (DayItems != null)
            {
                var item = DayItems.Where(x => DateOnly.FromDateTime(x.DateTime) == DateOnly.FromDateTime(args.Item.Date.Date)).FirstOrDefault();

                if (item != null)
                {
                    args.Item.DataContext = item;
                }
                else
                {
                    args.Item.DataContext = null;
                }
            }

            if (args.Phase == 0)
            {
                // Register callback for next phase.
                args.RegisterUpdateCallback(CalendarViewControl_CalendarViewDayItemChanging);
            }
            else if (args.Phase == 1)
            {
                // Blackout dates in the past, Sundays, and dates that are fully booked.
                if (args.Item.Date < DateTimeOffset.Now)
                {
                    args.Item.IsBlackout = true;
                }
                // Register callback for next phase.
                args.RegisterUpdateCallback(CalendarViewControl_CalendarViewDayItemChanging);
            }
        }
    }
}
