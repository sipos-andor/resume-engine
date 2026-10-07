using System.Globalization;

namespace Sipos.Resume.Core.Languages;

/// <summary>A language the CV is published in, as one content file defines it.</summary>
/// <param name="Tag">The BCP 47 tag from the file name, such as <c>hu</c> or <c>sr-Latn</c>; also the pages' <c>lang</c> and <c>hreflang</c>.</param>
/// <param name="Culture">The culture for dates and numbers, such as <c>hu-HU</c>.</param>
/// <param name="PathSegment">The first path segment of the language's pages, such as <c>sr</c>; empty for the default language.</param>
/// <param name="Endonym">The language's name in itself, such as <c>Magyar</c>, for the language menu.</param>
/// <param name="OgLocale">The Open Graph locale, such as <c>hu_HU</c>.</param>
/// <param name="IsDefault">Whether the language is the site's root and <c>x-default</c>.</param>
public sealed record ResumeLanguage(string Tag, CultureInfo Culture, string PathSegment, string Endonym, string OgLocale, bool IsDefault)
{
    /// <summary>The language's home page path: <c>/</c> for the default language, <c>/hu/</c> for another.</summary>
    public string HomePath => IsDefault ? "/" : $"/{PathSegment}/";

    /// <summary>A short uppercase code for file names, such as <c>HU</c> or <c>SR</c>.</summary>
    public string FileCode => Tag.Split('-')[0].ToUpperInvariant();
}
