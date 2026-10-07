namespace Sipos.Resume.Core.Site;

/// <summary>The settings of a CV site, the same in every language.</summary>
/// <param name="Origin">The canonical origin, such as <c>https://andor.sipos.io</c>.</param>
/// <param name="DefaultLanguage">The tag of the language served at the root, such as <c>en</c>.</param>
/// <param name="LanguageOrder">The order of the other languages in menus and lists; missing ones follow by tag.</param>
/// <param name="DownloadPrefix">The prefix of the download file names, such as <c>Andor_Sipos_CV</c>.</param>
/// <param name="ThemeStorageKey">The browser storage key of the reader's theme choice.</param>
/// <param name="LanguageStorageKey">The browser storage key of the reader's language choice.</param>
/// <param name="AnalyticsToken">A Cloudflare Web Analytics token, or <see langword="null"/> for no analytics.</param>
public sealed record SiteSettings(
    Uri Origin,
    string DefaultLanguage,
    IReadOnlyList<string> LanguageOrder,
    string DownloadPrefix,
    string ThemeStorageKey,
    string LanguageStorageKey,
    string? AnalyticsToken)
{
    /// <summary>Returns the absolute URL of a site path.</summary>
    /// <param name="path">A path starting with <c>/</c>.</param>
    public Uri Url(string path) => new(Origin, path);
}
