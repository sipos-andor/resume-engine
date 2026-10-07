namespace Sipos.Resume.Core.Content;

/// <summary>
/// The <c>site.json</c> of a CV: the settings that are the same in every language. The languages themselves come from
/// the content files.
/// </summary>
public sealed record SiteFile
{
    /// <summary>The JSON schema the file declares, if any.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("$schema")] public string? Schema { get; init; }

    /// <summary>The canonical origin, such as <c>https://andor.sipos.io</c>.</summary>
    public string? Origin { get; init; }

    /// <summary>The tag of the language served at the root, such as <c>en</c>.</summary>
    public string? DefaultLanguage { get; init; }

    /// <summary>The order of the other languages, by tag; missing ones follow by tag.</summary>
    public IReadOnlyList<string> LanguageOrder { get; set; } = [];

    /// <summary>The prefix of the download file names, such as <c>Andor_Sipos_CV</c>.</summary>
    public string? DownloadPrefix { get; init; }

    /// <summary>The browser storage key of the reader's theme choice.</summary>
    public string? ThemeStorageKey { get; init; }

    /// <summary>The browser storage key of the reader's language choice.</summary>
    public string? LanguageStorageKey { get; init; }
}
