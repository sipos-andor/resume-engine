using System.Globalization;
using Sipos.Resume.Core.Localization;

namespace Sipos.Resume.Core.Dates;

/// <summary>How a date is written for a reader.</summary>
public enum DateStyle
{
    /// <summary>The month by name in the culture's year-and-month form, such as <c>November 2022</c> or <c>2022. november</c>.</summary>
    Long,

    /// <summary>Digits only, month first, such as <c>11/2022</c>: the form applicant tracking systems parse best.</summary>
    Numeric,
}

/// <summary>Writes the dates and periods of a CV in its language, the same way on the page and in every document.</summary>
/// <remarks>
/// Decision: the culture's own year-and-month pattern for the designed layouts, and <c>MM/yyyy</c> for the ATS ones.
/// Why: <c>2022. november</c> and <c>studeni 2022.</c> are how a Hungarian or Croatian reader writes a month, which a
/// fixed pattern cannot give; an applicant tracking system, on the other hand, reads digits in every language.
/// Considered: abbreviated month names, which are uneven across cultures (<c>nov.</c>, <c>stu</c>, <c>Nov</c>).
/// </remarks>
public static class PeriodText
{
    /// <summary>The dash between the start and the end of a period, with a space on each side.</summary>
    public const string Separator = " – ";

    /// <summary>Writes a date: the year alone when only the year is known, otherwise the year and month.</summary>
    /// <param name="date">The date; a day, when known, is not shown.</param>
    /// <param name="culture">The CV's culture.</param>
    /// <param name="style">The form.</param>
    public static string Format(PartialDate date, CultureInfo culture, DateStyle style)
    {
        if (date.Month is not { } month)
        {
            return date.Year.ToString(CultureInfo.InvariantCulture);
        }

        return style == DateStyle.Numeric
            ? string.Create(CultureInfo.InvariantCulture, $"{month:00}/{date.Year}")
            : new DateTime(date.Year, month, 1).ToString(culture.DateTimeFormat.YearMonthPattern, culture);
    }

    /// <summary>Writes a period, such as <c>November 2022 – present</c> or <c>03/2015 – 10/2022</c>.</summary>
    /// <param name="period">The period.</param>
    /// <param name="labels">The CV's labels, whose culture and word for the present are used.</param>
    /// <param name="style">The form.</param>
    public static string Format(DateRange period, ResumeLabels labels, DateStyle style)
    {
        var start = Format(period.Start, labels.Culture, style);
        if (period.End is not { } end)
        {
            return start + Separator + labels.Present;
        }

        var last = Format(end, labels.Culture, style);
        return last == start ? start : start + Separator + last;
    }
}
