using System.Globalization;
using System.Text.RegularExpressions;

namespace Sipos.Resume.Core.Dates;

/// <summary>
/// A date as JSON Resume writes it: a year, a year and month, or a full date (<c>2025</c>, <c>2025-08</c>,
/// <c>2025-08-14</c>).
/// </summary>
/// <param name="Year">The year.</param>
/// <param name="Month">The month (1–12), or <see langword="null"/> when only the year is known.</param>
/// <param name="Day">The day of the month, or <see langword="null"/> when only the year or month is known.</param>
public readonly partial record struct PartialDate(int Year, int? Month = null, int? Day = null) : IComparable<PartialDate>
{
    /// <summary>The first day the date can stand for, such as 1 August for <c>2025-08</c>.</summary>
    public DateOnly FirstDay => new(Year, Month ?? 1, Day ?? 1);

    /// <summary>The last day the date can stand for, such as 31 August for <c>2025-08</c>.</summary>
    public DateOnly LastDay => Day is { } day
        ? new DateOnly(Year, Month!.Value, day)
        : Month is { } month
            ? new DateOnly(Year, month, DateTime.DaysInMonth(Year, month))
            : new DateOnly(Year, 12, 31);

    /// <summary>Reads a date in JSON Resume's form.</summary>
    /// <param name="text">The text, such as <c>2025-08</c>.</param>
    /// <param name="date">The date, when the text is one.</param>
    /// <returns><see langword="true"/> when the text is a valid date in one of the three forms.</returns>
    public static bool TryParse(string? text, out PartialDate date)
    {
        date = default;
        if (text is null || IsoForm().Match(text) is not { Success: true } match)
        {
            return false;
        }

        var year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
        int? month = match.Groups["month"].Success ? int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture) : null;
        int? day = match.Groups["day"].Success ? int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture) : null;
        // Year 0 matches the four digits but is no year DateOnly can hold.
        if (year < 1 || month is < 1 or > 12 || (day is { } d && (d < 1 || d > DateTime.DaysInMonth(year, month!.Value))))
        {
            return false;
        }

        date = new PartialDate(year, month, day);
        return true;
    }

    /// <summary>Reads a date in JSON Resume's form, and fails on anything else.</summary>
    /// <param name="text">The text, such as <c>2025-08</c>.</param>
    /// <exception cref="FormatException">The text is not <c>YYYY</c>, <c>YYYY-MM</c> or <c>YYYY-MM-DD</c>.</exception>
    public static PartialDate Parse(string text) =>
        TryParse(text, out var date) ? date : throw new FormatException($"'{text}' is not a date of the form YYYY, YYYY-MM or YYYY-MM-DD.");

    /// <summary>Compares by the first day each date can stand for.</summary>
    public int CompareTo(PartialDate other) => FirstDay.CompareTo(other.FirstDay);

    /// <summary>Returns the date in JSON Resume's form, with as many parts as it has.</summary>
    public override string ToString() => Day is not null
        ? FormattableString.Invariant($"{Year:D4}-{Month:D2}-{Day:D2}")
        : Month is not null ? FormattableString.Invariant($"{Year:D4}-{Month:D2}") : Year.ToString("D4", CultureInfo.InvariantCulture);

    /// <summary>Whether this date is earlier than another.</summary>
    public static bool operator <(PartialDate left, PartialDate right) => left.CompareTo(right) < 0;

    /// <summary>Whether this date is later than another.</summary>
    public static bool operator >(PartialDate left, PartialDate right) => left.CompareTo(right) > 0;

    /// <summary>Whether this date is earlier than or the same as another.</summary>
    public static bool operator <=(PartialDate left, PartialDate right) => left.CompareTo(right) <= 0;

    /// <summary>Whether this date is later than or the same as another.</summary>
    public static bool operator >=(PartialDate left, PartialDate right) => left.CompareTo(right) >= 0;

    [GeneratedRegex(@"^(?<year>\d{4})(-(?<month>\d{2})(-(?<day>\d{2}))?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoForm();
}
