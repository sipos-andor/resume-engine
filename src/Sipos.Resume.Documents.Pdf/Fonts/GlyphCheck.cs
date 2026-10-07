using System.Text;
using System.Text.RegularExpressions;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Documents.Pdf.Fonts;

/// <summary>Finds the characters of a CV that the bundled Plex fonts cannot draw, before any PDF is written.</summary>
/// <remarks>
/// QuestPDF has no fallback font and fails on such a character while it draws, such as an emoji, a star or a CJK
/// name, which the pages show through the browser's fallback. A theme that names fonts of its own is not checked
/// here; QuestPDF still reports them while drawing.
/// </remarks>
internal static partial class GlyphCheck
{
    /// <summary>Returns an issue for every text value with characters the fonts lack.</summary>
    /// <param name="edition">One language's content.</param>
    /// <param name="theme">The documents' look, or <see langword="null"/> for the bundled fonts.</param>
    public static IEnumerable<ValidationIssue> Check(ResumeEdition edition, DocumentTheme? theme)
    {
        ArgumentNullException.ThrowIfNull(edition);
        if (theme is not null && PlexFonts.Resolve(theme) is var resolved && (resolved.SansFamily != PlexFonts.Sans || resolved.MonoFamily != PlexFonts.Mono))
        {
            yield break;
        }

        foreach (var (pointer, text) in ContentStrings.Of(edition.Source))
        {
            // The designed PDF sets a position's or a project's technologies in Plex Mono, which has no Greek, say.
            var mono = MonoText().IsMatch(pointer);
            var missing = text.EnumerateRunes().Where(rune => !Rune.IsControl(rune) && !PlexFonts.Covers(rune.Value, mono)).Distinct().ToList();
            if (missing.Count > 0)
            {
                yield return new ValidationIssue(
                    edition.FileName,
                    pointer,
                    $"Has characters the PDF's fonts cannot draw: {string.Join(", ", missing.Select(rune => $"U+{rune.Value:X4} ({rune})"))}; leave them out or write them another way.");
            }
        }
    }

    [GeneratedRegex("^/(work/[0-9]+/x-keywords|projects/[0-9]+/keywords)/[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex MonoText();
}
