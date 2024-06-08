using Microsoft.UI.Xaml.Media;
using System;

namespace OpenSpartan.Workshop.Models
{
    public class SeasonCalendarViewDayItem
    {
        public SeasonCalendarViewDayItem(DateTime dateTime, string text, SolidColorBrush markerColor)
        {
            DateTime = dateTime;
            CSRSeasonText = text;
            CSRSeasonMarkerColor = markerColor;
        }

        public DateTime DateTime { get; }

        public string CSRSeasonText { get; }

        public SolidColorBrush CSRSeasonMarkerColor { get; set; }

        public string RegularSeasonText { get; set; }

        public SolidColorBrush RegularSeasonMarkerColor { get; set; }
    }
}
