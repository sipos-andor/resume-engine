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

    private static bool Register()
    {
        var assembly = typeof(PlexFonts).Assembly;
        foreach (var file in Files)
        {
            using var stream = assembly.GetManifestResourceStream($"Sipos.Resume.Documents.Pdf.Fonts.{file}")
                ?? throw new InvalidOperationException($"The embedded font {file} is missing from the package.");
            FontManager.RegisterFontFromStream(stream);
        }

        return true;
    }
}
