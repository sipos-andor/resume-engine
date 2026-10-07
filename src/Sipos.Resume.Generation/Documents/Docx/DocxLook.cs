using Sipos.Resume.Core.Artifacts;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>A font the document names, with what Word needs to substitute it on a machine that lacks it.</summary>
/// <param name="Name">The family name.</param>
/// <param name="Panose">The PANOSE classification as twenty hex digits, or <see langword="null"/> when unknown.</param>
/// <param name="Family">The generic family: <c>swiss</c>, <c>modern</c> or <c>roman</c>.</param>
/// <param name="Fixed">Whether every glyph has the same width.</param>
/// <param name="AltName">Another font to use in its place, or <see langword="null"/>.</param>
internal sealed record DocxFont(string Name, string? Panose, string Family, bool Fixed, string? AltName = null);

/// <summary>The fonts, colours and sizes of a layout; sizes are in half points, lengths in twentieths of a point.</summary>
/// <remarks>Margin is the left and right page margin, VerticalMargin the top and bottom one.</remarks>
/// <remarks>
/// Decision: fonts are named, not embedded, with their PANOSE numbers and generic family in the font table.
/// Why: embedding IBM Plex adds a megabyte to every file and makes Word open the document read-only for editing on
/// some setups; a named font with a PANOSE number falls back to a close sans on a machine without it.
/// </remarks>
internal sealed record DocxLook(
    DocumentVariant Variant,
    string Sans,
    string Mono,
    string Heading,
    string Body,
    string Muted,
    string Subtle,
    string Accent,
    string Rule,
    string Bullet,
    string BulletFont,
    string MeterFont,
    int BodySize,
    int SmallSize,
    int NameSize,
    int SubtitleSize,
    int Heading1Size,
    int Heading2Size,
    int Heading3Size,
    int BulletIndent,
    int Margin,
    int VerticalMargin,
    IReadOnlyList<DocxFont> Fonts)
{
    /// <summary>The width of an A4 page.</summary>
    public const int PageWidth = 11906;

    /// <summary>The height of an A4 page.</summary>
    public const int PageHeight = 16838;

    private static readonly DocxFont PlexSans = new("IBM Plex Sans", "020B0503050203000203", "swiss", Fixed: false);
    private static readonly DocxFont PlexMono = new("IBM Plex Mono", "020B0509050203000203", "modern", Fixed: true);
    private static readonly DocxFont CambriaMath = new("Cambria Math", "02040503050406030204", "roman", Fixed: false);
    private static readonly DocxFont Arial = new("Arial", "020B0604020202020204", "swiss", Fixed: false);
    private static readonly DocxFont Calibri = new("Calibri", "020F0502020204030204", "swiss", Fixed: false, AltName: "Arial");

    /// <summary>The width of the text between the margins, where right-aligned dates stop.</summary>
    public int TextWidth => PageWidth - (2 * Margin);

    /// <summary>Whether this is the designed layout.</summary>
    public bool IsDesigned => Variant == DocumentVariant.Designed;

    /// <summary>The designed look: the theme's colours and fonts, the ∧ bullet and the level meters.</summary>
    /// <param name="theme">The theme.</param>
    /// <remarks>
    /// Decision: the bullet is set in Cambria Math and the meter in Arial, not in the text font.
    /// Why: IBM Plex Sans has neither ∧ (U+2227) nor ● and ○; Word does not substitute a missing glyph reliably and
    /// draws a box, while Cambria Math and Arial ship with every Word on Windows and macOS.
    /// </remarks>
    public static DocxLook Designed(DocumentTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var sans = theme.SansFamily == PlexSans.Name ? PlexSans : new DocxFont(theme.SansFamily, Panose: null, "swiss", Fixed: false);
        var mono = theme.MonoFamily == PlexMono.Name ? PlexMono : new DocxFont(theme.MonoFamily, Panose: null, "modern", Fixed: true);
        return new DocxLook(
            DocumentVariant.Designed,
            sans.Name,
            mono.Name,
            theme.Heading,
            theme.Body,
            theme.Muted,
            theme.Subtle,
            theme.Accent,
            theme.Rule,
            Bullet: "∧",
            BulletFont: CambriaMath.Name,
            MeterFont: Arial.Name,
            BodySize: 19,
            SmallSize: 16,
            NameSize: 52,
            SubtitleSize: 23,
            Heading1Size: 17,
            Heading2Size: 22,
            Heading3Size: 20,
            BulletIndent: 284,
            Margin: 1134,
            VerticalMargin: 1021,
            Fonts: [sans, mono, CambriaMath, Arial]);
    }

    /// <summary>The ATS look: Calibri with Arial as its fallback, black and dark grey, a plain bullet.</summary>
    public static DocxLook Ats { get; } = new(
        DocumentVariant.Ats,
        Calibri.Name,
        Calibri.Name,
        Heading: "000000",
        Body: "1A1A1A",
        Muted: "404040",
        Subtle: "595959",
        Accent: "1A1A1A",
        Rule: "595959",
        Bullet: "•",
        BulletFont: Calibri.Name,
        MeterFont: Calibri.Name,
        BodySize: 21,
        SmallSize: 21,
        NameSize: 40,
        SubtitleSize: 24,
        Heading1Size: 26,
        Heading2Size: 23,
        Heading3Size: 22,
        BulletIndent: 360,
        Margin: 1134,
        VerticalMargin: 1134,
        Fonts: [Calibri, Arial]);
}
