using Microsoft.Extensions.Logging.Abstractions;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Generation.Building;
using Sipos.Resume.Generation.Output;

namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>Builds the two-language sample site into a folder.</summary>
internal static class Sites
{
    public const string Site = """
        {
          "origin": "https://cv.example.com",
          "defaultLanguage": "en",
          "languageOrder": ["hu"],
          "downloadPrefix": "Ann_Example_CV",
          "themeStorageKey": "cv-theme",
          "languageStorageKey": "cv-language"
        }
        """;

    public static void WriteContent(TempFolder folder)
    {
        folder.Write("content/site.json", Site);
        folder.Write("content/resume.en.json", Samples.English);
        folder.Write("content/resume.hu.json", Samples.Hungarian);
    }

    public static void WriteAssets(TempFolder folder)
    {
        folder.Write("wwwroot/js/site.js", "console.log('site');\n");
        folder.Write("wwwroot/js/site.js.br", "compressed");
    }

    public static ResumeSet Content() =>
        ContentLoader.Load(
            [new("site.json", System.Text.Encoding.UTF8.GetBytes(Site)), new("resume.en.json", System.Text.Encoding.UTF8.GetBytes(Samples.English)), new("resume.hu.json", System.Text.Encoding.UTF8.GetBytes(Samples.Hungarian))],
            analyticsToken: null).Set!;

    public static async Task<IReadOnlyList<SitePage>> BuildAsync(TempFolder folder, IResumeTheme theme, IReadOnlyList<IDocumentWriter> writers, string? email = null)
    {
        WriteAssets(folder);
        var options = new BuildOptions
        {
            ContentFolder = System.IO.Path.Combine(folder.Path, "content"),
            OutputFolder = System.IO.Path.Combine(folder.Path, "dist"),
            AssetsFolder = System.IO.Path.Combine(folder.Path, "wwwroot"),
            Today = SampleDocuments.Today,
            ContactEmail = email,
        };
        var builder = new SiteBuilder(theme, writers, shareImages: null, options, NullLoggerFactory.Instance);
        return await builder.BuildAsync(Content(), new FolderSink(options.OutputFolder), TestContext.Current.CancellationToken);
    }
}
