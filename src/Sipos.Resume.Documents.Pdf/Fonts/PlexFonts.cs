using QuestPDF.Drawing;
using Sipos.Resume.Core.Artifacts;

namespace Sipos.Resume.Documents.Pdf.Fonts;

/// <summary>The IBM Plex fonts the package embeds, registered with QuestPDF once per process.</summary>
internal static class PlexFonts
{
    /// <summary>The text family.</summary>
    public const string Sans = "IBM Plex Sans";

    /// <summary>The family for technologies.</summary>
    public const string Mono = "IBM Plex Mono";

    private static readonly string[] Files =
    [
        "IBMPlexSans-Regular.ttf", "IBMPlexSans-Medium.ttf", "IBMPlexSans-SemiBold.ttf",
        "IBMPlexMono-Regular.ttf",
    ];

    private static readonly Lazy<bool> Registration = new(Register, LazyThreadSafetyMode.ExecutionAndPublication);

    // The characters every Sans weight can draw, as a text may be set in any of them, and those Mono can draw.
    private static readonly Lazy<HashSet<int>> SansCoverage = new(
        () => Files.Where(file => file.StartsWith("IBMPlexSans", StringComparison.Ordinal)).Select(file => FontCoverage.Of(Read(file))).Aggregate((all, font) => { all.IntersectWith(font); return all; }),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<HashSet<int>> MonoCoverage = new(() => FontCoverage.Of(Read("IBMPlexMono-Regular.ttf")), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Registers the fonts unless done already; safe to call from several threads.</summary>
    public static void EnsureRegistered() => _ = Registration.Value;

    /// <summary>
    /// Returns a theme whose font families QuestPDF can draw: the theme's own when they are registered, by the host or
    /// by this package, otherwise Plex.
    /// </summary>
    /// <param name="theme">The theme as configured.</param>
    /// <remarks>
    /// Decision: an unknown family falls back to Plex instead of failing.
    /// Why: QuestPDF stops with an exception for a family it does not know, and a theme written for the web may name a
    /// font only the browser has.
    /// </remarks>
    public static DocumentTheme Resolve(DocumentTheme theme)
    {
        EnsureRegistered();
        var known = FontManager.GetRegisteredFonts().Select(font => font.FamilyName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return theme with
        {
            SansFamily = known.Contains(theme.SansFamily) ? theme.SansFamily : Sans,
            MonoFamily = known.Contains(theme.MonoFamily) ? theme.MonoFamily : Mono,
        };
    }

    /// <summary>Whether every weight of Plex Sans, and Plex Mono when asked, has a glyph for a character.</summary>
    /// <param name="codePoint">The character's code point.</param>
    /// <param name="mono">Whether the text is set in Plex Mono too, as the designed PDF sets technologies.</param>
    public static bool Covers(int codePoint, bool mono = false) =>
        SansCoverage.Value.Contains(codePoint) && (!mono || MonoCoverage.Value.Contains(codePoint));

    /// <summary>Whether the specified bundled family can draw a character, independently of the other family.</summary>
    public static bool CoversFamily(int codePoint, bool mono) =>
        (mono ? MonoCoverage.Value : SansCoverage.Value).Contains(codePoint);

    private static bool Register()
    {
        foreach (var file in Files)
        {
            using var stream = new MemoryStream(Read(file));
            FontManager.RegisterFontFromStream(stream);
        }

        return true;
    }

    private static byte[] Read(string file)
    {
        using var stream = typeof(PlexFonts).Assembly.GetManifestResourceStream($"Sipos.Resume.Documents.Pdf.Fonts.{file}")
            ?? throw new InvalidOperationException($"The embedded font {file} is missing from the package.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
