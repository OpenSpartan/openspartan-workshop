using Microsoft.UI.Xaml.Media;
using System;

namespace OpenSpartan.Workshop.Models
{
    public record SeasonCalendarViewDayItem
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
    }
}
