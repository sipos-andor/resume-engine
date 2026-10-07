using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Site;

namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>The pages of a two-language sample site: English at the root, Hungarian under /hu/.</summary>
internal static class Pages
{
    public static readonly SiteSettings Settings = new(new Uri("https://cv.example.com"), "en", ["hu"], "Ann_Example_CV", "cv-theme", "cv-language", null);

    public static IReadOnlyList<SitePage> Sample()
    {
        ResumeDocument[] documents = [SampleDocuments.English(), SampleDocuments.Hungarian()];
        var languages = documents.Select(document => document.Language).ToList();
        return [.. documents.Select(document => new SitePage(
            document,
            ResumeInsights.Analyze(document, SampleDocuments.Today),
            languages,
            DownloadCatalog.For(document.Language, Settings.DownloadPrefix, document.FocusProfiles.Select(profile => profile.Id)),
            Settings))];
    }
}
