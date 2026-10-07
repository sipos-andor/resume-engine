namespace Sipos.Resume.Core.Artifacts;

/// <summary>The look of the designed documents: colours as six-digit hex values and font family names.</summary>
/// <param name="Heading">Headings and the name.</param>
/// <param name="Body">Body text.</param>
/// <param name="Muted">Secondary text, such as dates.</param>
/// <param name="Subtle">Tertiary text and empty level marks.</param>
/// <param name="Accent">Accents: bullets, links, filled level marks.</param>
/// <param name="Rule">Lines between sections.</param>
/// <param name="SansFamily">The text font family, such as <c>IBM Plex Sans</c>.</param>
/// <param name="MonoFamily">The font family for technologies, such as <c>IBM Plex Mono</c>.</param>
public sealed record DocumentTheme(string Heading, string Body, string Muted, string Subtle, string Accent, string Rule, string SansFamily, string MonoFamily)
{
    /// <summary>A neutral theme for documents without a brand.</summary>
    public static DocumentTheme Neutral { get; } = new("111111", "222222", "555555", "888888", "1F5FAD", "DDDDDD", "IBM Plex Sans", "IBM Plex Mono");
}
