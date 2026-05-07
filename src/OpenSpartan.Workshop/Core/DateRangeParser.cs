using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenSpartan.Workshop.Core
{
    internal static class DateRangeParser
    {
        // Cache the compiled regex and the format array as static readonly so that
        // the per-call work is just regex matching, not regex construction + array
        // allocation. ExtractDateRanges is invoked once per season/operation/event
        // during calendar load (~30+ calls per refresh).
        private static readonly Regex DateRangeRegex = new(
            @"(?<startMonth>\w+)\s(?<startDay>\d{1,2})(?:st|nd|rd|th)?,?\s(?<startYear>\d{4})(?:\s-\s(?<endMonth>\w+)\s(?<endDay>\d{1,2})(?:st|nd|rd|th)?,?\s(?<endYear>\d{4}))?",
            RegexOptions.Compiled);

        private static readonly Regex OrdinalSuffixRegex = new(@"(st|nd|rd|th)", RegexOptions.Compiled);

        private static readonly string[] DateFormats =
        {
            "MMMM d, yyyy", "MMM d, yyyy", "MMMM d yyyy", "MMM d yyyy",
            "MMMM dd, yyyy", "MMM dd, yyyy", "MMMM dd yyyy", "MMM dd yyyy",
        };

        internal static List<Tuple<DateTime, DateTime>> ExtractDateRanges(string input)
        {
            // There is a typo in one of the date range definitions, so we want to
            // work around it by replacing it with the proper string.
            input = input.Replace("Febraury", "February", StringComparison.OrdinalIgnoreCase)
                         .Replace("Sept ", "Sep ", StringComparison.OrdinalIgnoreCase);

            return DateRangeRegex.Matches(input).Cast<Match>()
                .Select(match =>
                {
                    string startDay = OrdinalSuffixRegex.Replace(match.Groups["startDay"].Value, "");
                    string endDay = match.Groups["endDay"].Success ? OrdinalSuffixRegex.Replace(match.Groups["endDay"].Value, "") : null;

                    string startDateStr = $"{match.Groups["startMonth"].Value} {startDay}, {match.Groups["startYear"].Value}";
                    string endDateStr = match.Groups["endMonth"].Success
                        ? $"{match.Groups["endMonth"].Value} {endDay}, {match.Groups["endYear"].Value}"
                        : startDateStr;

                    bool startDateParsed = DateTime.TryParseExact(startDateStr, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate);
                    bool endDateParsed = DateTime.TryParseExact(endDateStr, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDate);

                    if (startDateParsed && endDateParsed)
                    {
                        // Open-ended ranges (e.g. "November 18, 2026" with no `- endDate`
                        // portion, like the Infinite operation that is the last season
                        // and has no scheduled end) collapse to a single day under the
                        // regex fallback. Detect that and extend so the calendar marker
                        // covers any reasonable view window — a year past today, or a
                        // year past the start when the start is itself in the future.
                        if (!match.Groups["endMonth"].Success)
                        {
                            var openEndedEnd = startDate > DateTime.UtcNow.Date
                                ? startDate.AddYears(1)
                                : DateTime.UtcNow.Date.AddYears(1);
                            endDate = openEndedEnd;
                        }
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
