using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Timeline;

/// <summary>Whether a bar of the timeline is a position or a project.</summary>
public enum TimelineBarKind
{
    /// <summary>A position.</summary>
    Position,

    /// <summary>A project.</summary>
    Engagement,
}

/// <summary>A bar of the timeline.</summary>
/// <param name="Id">The item's anchor, which a click on the bar leads to.</param>
/// <param name="Label">The bar's text.</param>
/// <param name="Start">The first day.</param>
/// <param name="End">The last day: the end date's last day, or the timeline's end for an ongoing item.</param>
/// <param name="Ongoing">Whether the item is still going on.</param>
/// <param name="Kind">Position or project.</param>
/// <param name="Row">The row within its kind, so overlapping bars do not cover each other.</param>
public sealed record TimelineBar(string Id, string Label, DateOnly Start, DateOnly End, bool Ongoing, TimelineBarKind Kind, int Row);

/// <summary>The CV's positions and projects on one time axis, with overlapping items on separate rows.</summary>
/// <param name="Start">The first day of the axis: the first of January of the earliest year.</param>
/// <param name="End">The last day of the axis: the present.</param>
/// <param name="PositionRows">How many rows the positions take.</param>
/// <param name="EngagementRows">How many rows the projects take.</param>
/// <param name="Bars">The bars, positions first, each kind by start date.</param>
public sealed record TimelineModel(DateOnly Start, DateOnly End, int PositionRows, int EngagementRows, IReadOnlyList<TimelineBar> Bars)
{
    /// <summary>Lays out the dated positions and projects of a CV.</summary>
    /// <param name="document">The CV.</param>
    /// <param name="today">The date that stands for the present, so the layout does not depend on the clock.</param>
    public static TimelineModel Build(ResumeDocument document, DateOnly today)
    {
        var positions = Rows(document.Positions.Select(p => (p.Id, Label: p.Organization, p.Period)), TimelineBarKind.Position, today);
        var engagements = Rows(
            document.AllEngagements.Where(e => e.Period is not null).Select(e => (e.Id, Label: e.Client is null ? e.Name : $"{e.Client} – {e.Name}", Period: e.Period!.Value)),
            TimelineBarKind.Engagement,
            today);
        var bars = positions.Concat(engagements).ToList();
        var start = bars.Count == 0 ? today : new DateOnly(bars.Min(bar => bar.Start).Year, 1, 1);
        return new TimelineModel(
            start,
            today,
            positions.Count == 0 ? 0 : positions.Max(bar => bar.Row) + 1,
            engagements.Count == 0 ? 0 : engagements.Max(bar => bar.Row) + 1,
            bars);
    }

    // Interval partitioning: each bar goes to the first row whose last bar ended before it starts.
    private static List<TimelineBar> Rows(IEnumerable<(string Id, string Label, Dates.DateRange Period)> items, TimelineBarKind kind, DateOnly today)
    {
        var rowEnds = new List<DateOnly>();
        var bars = new List<TimelineBar>();
        foreach (var (id, label, period) in items.OrderBy(item => item.Period.Start.FirstDay))
        {
            var start = period.Start.FirstDay;
            var end = period.LastDay(today);
            var row = rowEnds.FindIndex(rowEnd => rowEnd < start);
            if (row < 0)
            {
                rowEnds.Add(end);
                row = rowEnds.Count - 1;
            }
            else
            {
                rowEnds[row] = end;
            }

            bars.Add(new TimelineBar(id, label, start, end, period.IsOngoing, kind, row));
        }

        return bars;
    }
}
