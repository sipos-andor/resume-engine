using System.Globalization;
using System.Net;
using System.Text;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Timeline;

namespace Sipos.Resume.Theme.Operandor.Rendering;

/// <summary>
/// Draws the engine's timeline (<see cref="TimelineModel"/>) as SVG: a tick per year, positions on top, projects below,
/// each bar a link to its item.
/// </summary>
/// <remarks>
/// <para>
/// Decision: the SVG is written as text, with every number in the invariant culture.
/// Why: Razor reads an SVG <c>&lt;text&gt;</c> element as its own tag, and the page's culture would write a decimal
/// comma into the coordinates.
/// </para>
/// <para>
/// Decision: no viewBox; the horizontal positions and lengths are percentages of the chart's width, the vertical ones
/// pixels.
/// Why: a viewBox scales the text with the drawing, so the 12px years shrank to 4px on a phone; this way the bars
/// follow the column's width and the text keeps its size.
/// </para>
/// </remarks>
internal static class TimelineSvg
{
    // The drawing's horizontal units, which P writes as percentages.
    private const double Width = 1000;
    private const double BarHeight = 22;
    private const double RowGap = 6;
    private const double GroupGap = 14;
    private const double AxisHeight = 22;
    private const double CharacterWidth = 6.4;

    // The narrowest chart, in pixels, that shows the bars' labels: below 640px the page hides them.
    private const double LabelledWidth = 600;

    /// <summary>Returns the SVG element of a timeline.</summary>
    /// <param name="model">The timeline.</param>
    /// <param name="href">The link of an item, by its identifier.</param>
    /// <param name="period">A period in the page's words.</param>
    public static string Draw(TimelineModel model, Func<string, string> href, Func<DateRange, string> period)
    {
        var height = (model.PositionRows + model.EngagementRows) * (BarHeight + RowGap) + (model.EngagementRows > 0 ? GroupGap : 0) + AxisHeight;
        var span = model.End.DayNumber - model.Start.DayNumber + 1;
        double X(DateOnly date) => Math.Clamp((date.DayNumber - model.Start.DayNumber) * Width / span, 0, Width);
        double Top(TimelineBar bar) => bar.Kind == TimelineBarKind.Position
            ? bar.Row * (BarHeight + RowGap)
            : (model.PositionRows * (BarHeight + RowGap)) + GroupGap + (bar.Row * (BarHeight + RowGap));
        var labelEvery = model.End.Year - model.Start.Year > 12 ? 2 : 1;

        var svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture, $"<svg class=\"cv-timeline__chart\" width=\"100%\" height=\"{N(height)}\" aria-hidden=\"true\" focusable=\"false\">");
        for (var year = model.Start.Year; year <= model.End.Year; year++)
        {
            var x = X(new DateOnly(year, 1, 1));
            svg.Append(CultureInfo.InvariantCulture, $"<line class=\"cv-timeline__tick\" x1=\"{P(x)}\" y1=\"0\" x2=\"{P(x)}\" y2=\"{N(height - AxisHeight)}\"/>");
            if ((model.End.Year - year) % labelEvery == 0)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<text class=\"cv-timeline__year\" x=\"{P(x)}\" dx=\"3\" y=\"{N(height - 6)}\">{year}</text>");
            }
        }

        foreach (var bar in model.Bars)
        {
            var x = X(bar.Start);
            var end = bar.End == DateOnly.MaxValue ? Width : X(bar.End.AddDays(1));
            var width = Math.Max(end - x, 3);
            var y = Top(bar);
            var kind = bar.Kind == TimelineBarKind.Position ? "position" : "engagement";
            var ongoing = bar.Ongoing ? " cv-timeline__bar--ongoing" : "";
            // The CV's own period: a year given alone stays a year, as in the item's text.
            var when = period(bar.Period);
            svg.Append(CultureInfo.InvariantCulture, $"<a href=\"{E(href(bar.Id))}\" tabindex=\"-1\" class=\"cv-timeline__link\" data-cv-bar=\"{E(bar.Id)}\">");
            svg.Append(CultureInfo.InvariantCulture, $"<title>{E(bar.Label)}: {E(when)}</title>");
            svg.Append(CultureInfo.InvariantCulture, $"<rect class=\"cv-timeline__bar cv-timeline__bar--{kind}{ongoing}\" x=\"{P(x)}\" y=\"{N(y)}\" width=\"{P(width)}\" height=\"{N(BarHeight)}\" rx=\"3\"/>");
            if (FitsLabel(width, bar.Label))
            {
                svg.Append(CultureInfo.InvariantCulture, $"<text class=\"cv-timeline__label\" x=\"{P(x)}\" dx=\"6\" y=\"{N(y + BarHeight - 7)}\">{E(bar.Label)}</text>");
            }

            svg.Append("</a>");
        }

        return svg.Append("</svg>").ToString();
    }

    /// <summary>Whether a bar is wide enough to carry its label, 6px in from its start, on every chart that shows labels.</summary>
    /// <param name="width">The bar's width in the drawing's units (of <see cref="Width"/>).</param>
    /// <param name="label">The bar's label, drawn in 12px text of about <see cref="CharacterWidth"/> pixels a character.</param>
    /// <remarks>
    /// Decision: a label is measured against the narrowest chart that shows labels, not the widest.
    /// Why: the same SVG serves every width, so a label that fits only a wide chart would run out of its bar on a
    /// narrower one; a wide chart rather leaves a short bar without its label, whose item the list below names.
    /// </remarks>
    private static bool FitsLabel(double width, string label) =>
        width / Width * LabelledWidth > (label.Length * CharacterWidth) + 12;

    private static string N(double value) => Math.Round(value, 1).ToString(CultureInfo.InvariantCulture);

    // A horizontal position or length as a percentage of the chart's width.
    private static string P(double value) => Math.Round(value * 100 / Width, 2).ToString(CultureInfo.InvariantCulture) + "%";

    private static string E(string text) => WebUtility.HtmlEncode(text);
}
