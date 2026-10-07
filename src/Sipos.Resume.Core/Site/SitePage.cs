using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Site;

/// <summary>Everything a theme renders a CV's page from.</summary>
/// <param name="Document">The CV in the page's language.</param>
/// <param name="Insights">What the engine derived from it.</param>
/// <param name="Languages">Every language of the site, default first.</param>
/// <param name="Downloads">The page language's downloads.</param>
/// <param name="Settings">The site's settings.</param>
/// <param name="ShareImagePath">The path of the page's share image, or <see langword="null"/> when the build draws none.</param>
public sealed record SitePage(
    ResumeDocument Document,
    ResumeInsights Insights,
    IReadOnlyList<ResumeLanguage> Languages,
    IReadOnlyList<DownloadSpec> Downloads,
    SiteSettings Settings,
    string? ShareImagePath = null)
{
    /// <summary>The page's canonical path, such as <c>/hu/</c>.</summary>
    public string Path => Document.Language.HomePath;

    /// <summary>The path of the page's Markdown version, such as <c>/hu/index.md</c>.</summary>
    public string MarkdownPath => Path + "index.md";

    /// <summary>The path of the page's JSON Resume, such as <c>/hu/resume.json</c>.</summary>
    public string JsonResumePath => Path + "resume.json";

    /// <summary>The path of the page's llms.txt, such as <c>/hu/llms.txt</c>.</summary>
    public string LlmsTxtPath => Path + "llms.txt";
}
