namespace Sipos.Resume.Core.Dates;

/// <summary>A period from a start date to an end date, or to the present when the end is not set.</summary>
/// <param name="Start">The first date of the period.</param>
/// <param name="End">The last date of the period, or <see langword="null"/> for an ongoing one.</param>
public readonly record struct DateRange(PartialDate Start, PartialDate? End = null)
{
    /// <summary>Whether the period is still going on.</summary>
    public bool IsOngoing => End is null;

    /// <summary>The last day of the period: the end date's last day, or <paramref name="today"/> for an ongoing one.</summary>
    /// <param name="today">The date that stands for the present, so the result does not depend on the clock.</param>
    public DateOnly LastDay(DateOnly today) => End?.LastDay ?? today;

    /// <summary>Whether two periods share at least one day.</summary>
    /// <param name="other">The other period.</param>
    /// <param name="today">The date that stands for the present.</param>
    public bool Overlaps(DateRange other, DateOnly today) =>
        Start.FirstDay <= other.LastDay(today) && other.Start.FirstDay <= LastDay(today);
}
