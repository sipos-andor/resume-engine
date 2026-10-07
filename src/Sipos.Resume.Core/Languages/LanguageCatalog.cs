using System.Globalization;
using System.Text.RegularExpressions;
using Sipos.Resume.Core.Content;

namespace Sipos.Resume.Core.Languages;

/// <summary>Derives the site's languages from its content files: <c>resume.hu.json</c> publishes Hungarian.</summary>
/// <remarks>
/// Decision: a language exists when its content file does; its culture, name, Open Graph locale and path come from the
/// tag, and the file's <c>meta</c> may override each.
/// Why: one list to keep in step instead of two; adding a language is adding a file.
/// Considered: a list of languages in the site configuration, which can name a language without a file or miss one.
/// </remarks>
public static partial class LanguageCatalog
{
    /// <summary>Returns the language tag a content file's name declares, or <see langword="null"/> for another file.</summary>
    /// <param name="fileName">A file name, such as <c>resume.sr-Latn.json</c>.</param>
    public static string? TagOf(string fileName) =>
        ContentFile().Match(Path.GetFileName(fileName)) is { Success: true } match ? match.Groups["tag"].Value : null;

    /// <summary>Describes the language of one content file.</summary>
    /// <param name="tag">The tag from the file name.</param>
    /// <param name="meta">The file's <c>meta</c>, whose <c>x-</c> values override the derived ones.</param>
    /// <param name="isDefault">Whether this is the site's default language.</param>
    /// <exception cref="CultureNotFoundException">The tag or the culture override names no known culture.</exception>
    public static ResumeLanguage Describe(string tag, JsonResumeMeta? meta, bool isDefault)
    {
        var culture = meta?.Culture is { Length: > 0 } name ? CultureInfo.GetCultureInfo(name) : CultureInfo.CreateSpecificCulture(tag);
        var segment = isDefault ? "" : meta?.Path is { Length: > 0 } path ? path : tag.Split('-')[0].ToLowerInvariant();
        var endonym = meta?.Endonym is { Length: > 0 } own ? own : Capitalize(CultureInfo.GetCultureInfo(tag).NativeName, culture);
        var locale = meta?.OgLocale is { Length: > 0 } og ? og : OgLocaleOf(culture);
        return new ResumeLanguage(tag, culture, segment, endonym, locale, isDefault);
    }

    /// <summary>Orders languages: the default first, then the given order, then the rest by tag.</summary>
    /// <param name="languages">The languages to order.</param>
    /// <param name="order">Preferred order of tags after the default one; tags without a language are ignored.</param>
    public static IReadOnlyList<ResumeLanguage> Order(IEnumerable<ResumeLanguage> languages, IReadOnlyList<string> order)
    {
        int Rank(ResumeLanguage language)
        {
            var index = order.ToList().FindIndex(tag => string.Equals(tag, language.Tag, StringComparison.OrdinalIgnoreCase));
            return language.IsDefault ? -1 : index < 0 ? int.MaxValue : index;
        }

        return [.. languages.OrderBy(Rank).ThenBy(language => language.Tag, StringComparer.OrdinalIgnoreCase)];
    }

    private static string OgLocaleOf(CultureInfo culture)
    {
        try
        {
            return $"{culture.TwoLetterISOLanguageName}_{new RegionInfo(culture.Name).TwoLetterISORegionName}";
        }
        catch (ArgumentException)
        {
            return culture.TwoLetterISOLanguageName;
        }
    }

    private static string Capitalize(string name, CultureInfo culture) =>
        name.Length == 0 ? name : char.ToUpper(name[0], culture) + name[1..];

    [GeneratedRegex(@"^resume\.(?<tag>[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*)\.json$", RegexOptions.CultureInvariant)]
    private static partial Regex ContentFile();
}
