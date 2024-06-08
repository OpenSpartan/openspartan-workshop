using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenSpartan.Workshop.Core
{
    internal class DateRangeParser
    {
        internal static List<Tuple<DateTime, DateTime>> ExtractDateRanges(string input)
        {
            // There is a typo in one of the date range definitions, so we want to
            // work around it by replacing it with the proper string.
            input = input.Replace("Febraury", "February", StringComparison.OrdinalIgnoreCase);

            string pattern = @"(?<startMonth>\w+)\s(?<startDay>\d{1,2})(?:st|nd|rd|th)?,?\s(?<startYear>\d{4})(?:\s-\s(?<endMonth>\w+)\s(?<endDay>\d{1,2})(?:st|nd|rd|th)?,?\s(?<endYear>\d{4}))?";
            Regex regex = new Regex(pattern);
            string[] dateFormats = { "MMMM d, yyyy", "MMM d, yyyy", "MMMM d yyyy", "MMM d yyyy" };

            return regex.Matches(input).Cast<Match>()
                .Select(match =>
                {
                    string startDay = Regex.Replace(match.Groups["startDay"].Value, @"(st|nd|rd|th)", "");
                    string endDay = Regex.Replace(match.Groups["endDay"].Value, @"(st|nd|rd|th)", "");

                    string startDateStr = $"{match.Groups["startMonth"].Value} {startDay}, {match.Groups["startYear"].Value}";
                    string endDateStr = match.Groups["endMonth"].Success
                        ? $"{match.Groups["endMonth"].Value} {endDay}, {match.Groups["endYear"].Value}"
                        : startDateStr;

                    if (DateTime.TryParseExact(startDateStr, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate) &&
                        DateTime.TryParseExact(endDateStr, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDate))
                    {
                        return new Tuple<DateTime, DateTime>(startDate, endDate);
                    }
                    else
                    {
                        throw new FormatException($"Invalid date format encountered. {input}");
                    }
                })
                .ToList();
        }
    }
}
