using System.Globalization;
using System.Text;

namespace Sipos.Resume.Documents.Pdf.Marks;

/// <summary>The small vector marks of the designed layout, as SVG text QuestPDF draws as paths.</summary>
/// <remarks>
/// Decision: marks are vector paths, not font glyphs.
/// Why: a glyph such as ∧ or ■ lands in the text layer, where an applicant tracking system or a screen reader reads it
/// as content; paths marked as artifacts stay out of both, and do not depend on a font having the glyph.
/// </remarks>
internal static class VectorMarks
{
    /// <summary>The width to height ratio of <see cref="Chevron"/>.</summary>
    public const float ChevronRatio = 12f / 8f;

    /// <summary>The number of steps of a level meter.</summary>
    public const int MeterSteps = 5;

    /// <summary>The width to height ratio of <see cref="Meter"/>.</summary>
    public const float MeterRatio = (MeterSteps * Square + (MeterSteps - 1) * Gap) / Square;

    private const float Square = 10f;
    private const float Gap = 4f;

    /// <summary>The upward chevron of the operandor mark, as a bullet: a stroke with butt ends and a mitred tip.</summary>
    /// <param name="color">The stroke colour, six hex digits.</param>
    public static string Chevron(string color) =>
        $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 12 8"><path d="M1.2 7.2 L6 2.4 L10.8 7.2" fill="none" stroke="#{color}" stroke-width="2.2" stroke-linecap="butt" stroke-linejoin="miter"/></svg>""";

    /// <summary>A level meter: <paramref name="rating"/> filled squares out of <see cref="MeterSteps"/>.</summary>
    /// <param name="rating">The level from 1 to 5; values outside are clamped.</param>
    /// <param name="filled">The colour of the reached steps.</param>
    /// <param name="empty">The colour of the other steps.</param>
    public static string Meter(int rating, string filled, string empty)
    {
        var level = Math.Clamp(rating, 0, MeterSteps);
        var width = MeterSteps * Square + (MeterSteps - 1) * Gap;
        var svg = new StringBuilder().Append(CultureInfo.InvariantCulture, $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {Square}">""");
        for (var step = 0; step < MeterSteps; step++)
        {
            svg.Append(CultureInfo.InvariantCulture, $"""<rect x="{step * (Square + Gap)}" y="0" width="{Square}" height="{Square}" rx="1.5" fill="#{(step < level ? filled : empty)}"/>""");
        }

        return svg.Append("</svg>").ToString();
    }
}
