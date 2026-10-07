using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Localization;
using Sipos.Resume.Core.Mapping;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Content;

/// <summary>One language of a CV: the content file as read and the document mapped from it.</summary>
/// <param name="FileName">The content file, such as <c>resume.hu.json</c>.</param>
/// <param name="Source">The file as read, which the JSON Resume download republishes.</param>
/// <param name="Document">The document in the domain's terms.</param>
public sealed record ResumeEdition(string FileName, JsonResume Source, ResumeDocument Document)
{
    /// <summary>The edition's language.</summary>
    public ResumeLanguage Language => Document.Language;
}

/// <summary>A CV's content, read and checked: the site's settings and every language, the default first.</summary>
/// <param name="Settings">The site's settings.</param>
/// <param name="Editions">The languages, the default first, then in the site's order.</param>
public sealed record ResumeSet(SiteSettings Settings, IReadOnlyList<ResumeEdition> Editions)
{
    /// <summary>The site's languages, the default first.</summary>
    public IReadOnlyList<ResumeLanguage> Languages => [.. Editions.Select(edition => edition.Language)];
}

/// <summary>The outcome of loading a CV's content: the set when nothing is wrong, and every issue found.</summary>
/// <param name="Set">The content, or <see langword="null"/> when any issue was found.</param>
/// <param name="Issues">The problems, each located by file and JSON pointer.</param>
public sealed record LoadResult(ResumeSet? Set, IReadOnlyList<ValidationIssue> Issues);

/// <summary>Loads a CV's content files into a checked <see cref="ResumeSet"/>: the use case behind every build.</summary>
/// <remarks>
/// <para>
/// Decision: the loader reports every issue of every file in one pass and returns no set while any remains.
/// Why: a translator who fixes one issue per build run needs as many runs as there are issues; and a half-valid set
/// would publish a site where one language silently lacks what the others have.
/// </para>
/// <para>
/// Decision: it lives in the core, not in the build.
/// Why: the Studio checks a CV in the browser with the same rules, and a CV that passes there must build.
/// </para>
/// </remarks>
public static partial class ContentLoader
{
    /// <summary>The name of the site's settings file.</summary>
    public const string SiteFileName = "site.json";

    /// <summary>Reads, checks and maps the content files.</summary>
    /// <param name="files">The content files; files that are neither <c>site.json</c> nor <c>resume.{language}.json</c> are ignored.</param>
    /// <param name="analyticsToken">A Cloudflare Web Analytics token, or <see langword="null"/>; it comes from the build's secrets, not the content.</param>
    public static LoadResult Load(IReadOnlyList<ContentFile> files, string? analyticsToken)
    {
        ArgumentNullException.ThrowIfNull(files);
        var issues = new List<ValidationIssue>();
        var site = ReadSite(files, issues);

        var resumes = new List<(string Name, string Tag, JsonResume Resume)>();
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files.Where(file => file.Name.StartsWith("resume.", StringComparison.Ordinal)).OrderBy(file => file.Name, StringComparer.Ordinal))
        {
            if (LanguageCatalog.TagOf(file.Name) is not { } tag)
            {
                issues.Add(new ValidationIssue(file.Name, "", "The name must be resume.{language}.json with a BCP 47 language tag, such as resume.hu.json."));
                continue;
            }

            // BCP 47 tags ignore case, so resume.en.json and resume.EN.json are one language twice.
            if (!tags.TryAdd(tag, file.Name))
            {
                issues.Add(new ValidationIssue(file.Name, "", $"Is the same language as {tags[tag]}; keep one file per language."));
                continue;
            }

            var read = ResumeReader.Read(file.Name, file.Content);
            issues.AddRange(read.Issues);
            if (read.Resume is { } resume)
            {
                issues.AddRange(ResumeValidator.Validate(file.Name, resume));
                resumes.Add((file.Name, tag, resume));
            }
        }

        if (resumes.Count == 0)
        {
            issues.Add(new ValidationIssue("content", "", "No resume.{language}.json file was found."));
            return new LoadResult(null, issues);
        }

        if (site?.DefaultLanguage is null)
        {
            return new LoadResult(null, issues);
        }

        var reference = resumes.FirstOrDefault(resume => string.Equals(resume.Tag, site.DefaultLanguage, StringComparison.OrdinalIgnoreCase));
        if (reference.Resume is null)
        {
            issues.Add(new ValidationIssue(SiteFileName, "/defaultLanguage", $"There is no resume.{site.DefaultLanguage}.json for the default language."));
            return new LoadResult(null, issues);
        }

        issues.AddRange(ParityValidator.Compare((reference.Name, reference.Resume), resumes.Where(resume => resume.Name != reference.Name).Select(resume => (resume.Name, resume.Resume))));

        var editions = new List<ResumeEdition>();
        foreach (var (name, tag, resume) in resumes)
        {
            ResumeLanguage language;
            try
            {
                language = LanguageCatalog.Describe(tag, resume.Meta, isDefault: name == reference.Name);
            }
            catch (CultureNotFoundException)
            {
                issues.Add(new ValidationIssue(name, "", $"'{resume.Meta?.Culture ?? tag}' is no culture this system knows."));
                continue;
            }

            if (!ResumeLabels.Supports(language.Culture))
            {
                issues.Add(new ValidationIssue(name, "", $"The engine has no labels in {language.Culture.EnglishName}; add ResumeLabels.{tag}.resx."));
            }

            editions.Add(new ResumeEdition(name, resume, ResumeMapper.Map(resume, language)));
        }

        foreach (var clash in editions.GroupBy(edition => edition.Language.HomePath, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            issues.Add(new ValidationIssue(clash.Last().FileName, "/meta/x-path", $"Another language is already published under {clash.Key}."));
        }

        if (issues.Count > 0)
        {
            return new LoadResult(null, issues);
        }

        var languages = LanguageCatalog.Order(editions.Select(edition => edition.Language), site.LanguageOrder);
        var settings = new SiteSettings(
            new Uri(site.Origin!, UriKind.Absolute),
            reference.Tag,
            site.LanguageOrder,
            site.DownloadPrefix!,
            site.ThemeStorageKey!,
            site.LanguageStorageKey!,
            string.IsNullOrWhiteSpace(analyticsToken) ? null : analyticsToken.Trim());
        return new LoadResult(new ResumeSet(settings, [.. languages.Select(language => editions.First(edition => edition.Language == language))]), issues);
    }

    // Returns the settings whenever the file could be read, so the content files are checked against its default
    // language even while a setting is wrong; the issues keep the load from returning a set.
    private static SiteFile? ReadSite(IReadOnlyList<ContentFile> files, List<ValidationIssue> issues)
    {
        if (files.FirstOrDefault(file => file.Name == SiteFileName) is not { } file)
        {
            issues.Add(new ValidationIssue(SiteFileName, "", "The site's settings file is missing."));
            return null;
        }

        SiteFile? site;
        try
        {
            site = JsonSerializer.Deserialize(ResumeReader.WithoutByteOrderMark(file.Content).Span, ResumeJsonContext.Default.SiteFile);
        }
        catch (JsonException exception)
        {
            issues.Add(new ValidationIssue(SiteFileName, "", $"Not valid JSON of the expected shape: {exception.Message}"));
            return null;
        }

        if (site is null)
        {
            issues.Add(new ValidationIssue(SiteFileName, "", "The file must hold a JSON object."));
            return null;
        }

        if (!Uri.TryCreate(site.Origin, UriKind.Absolute, out var origin) || origin.Scheme is not ("https" or "http") || origin.PathAndQuery != "/" || origin.Fragment.Length > 0)
        {
            issues.Add(new ValidationIssue(SiteFileName, "/origin", "Must be an absolute http(s) origin without a path, such as https://cv.example.com."));
        }

        if (string.IsNullOrWhiteSpace(site.DefaultLanguage))
        {
            issues.Add(new ValidationIssue(SiteFileName, "/defaultLanguage", "Must name the language served at the root, such as en."));
        }

        if (site.DownloadPrefix is null || !DownloadPrefix().IsMatch(site.DownloadPrefix))
        {
            issues.Add(new ValidationIssue(SiteFileName, "/downloadPrefix", "Must be ASCII letters, digits, '_' or '-', such as Ann_Example_CV."));
        }

        if (string.IsNullOrWhiteSpace(site.ThemeStorageKey))
        {
            issues.Add(new ValidationIssue(SiteFileName, "/themeStorageKey", "Must name the browser storage key of the theme choice."));
        }

        if (string.IsNullOrWhiteSpace(site.LanguageStorageKey))
        {
            issues.Add(new ValidationIssue(SiteFileName, "/languageStorageKey", "Must name the browser storage key of the language choice."));
        }

        return site;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex DownloadPrefix();
}
