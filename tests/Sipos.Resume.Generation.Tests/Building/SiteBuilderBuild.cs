using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Generation.Documents.Json;
using Sipos.Resume.Generation.Documents.Markdown;
using Sipos.Resume.Generation.Documents.Text;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Building;

public class SiteBuilderBuild
{
    private static IDocumentWriter[] Writers(params IDocumentWriter[] more) => [.. more, new MarkdownDocumentWriter(), new PlainTextDocumentWriter(), new JsonResumeDocumentWriter()];

    [Fact]
    public async Task WritesPagesMachineFilesAndDownloadsGivenTwoLanguages()
    {
        using var folder = new TempFolder();

        await Sites.BuildAsync(folder, new FakeTheme(), Writers());

        foreach (var file in new[]
        {
            "dist/index.html", "dist/hu/index.html", "dist/404.html", "dist/sitemap.xml", "dist/robots.txt", "dist/llms.txt", "dist/hu/llms.txt",
            "dist/llms-full.txt", "dist/index.md", "dist/hu/index.md", "dist/resume.json", "dist/hu/resume.json", "dist/site-config.js",
            "dist/js/site.js", "dist/.nojekyll", "dist/downloads/Ann_Example_CV_EN.md", "dist/downloads/Ann_Example_CV_HU_ATS.txt",
        })
        {
            folder.Exists(file).ShouldBeTrue(file);
        }

        folder.Exists("dist/js/site.js.br").ShouldBeFalse();
        folder.Read("dist/index.md").ShouldBe(folder.Read("dist/downloads/Ann_Example_CV_EN.md"));
        folder.Read("dist/llms-full.txt").ShouldContain("# Example Ann");
    }

    // A download is offered only when a writer can write it, so the page never links to a missing file.
    [Fact]
    public async Task OffersOnlyDownloadsWithWriterGivenNoPdfOrDocxWriter()
    {
        using var folder = new TempFolder();

        var pages = await Sites.BuildAsync(folder, new FakeTheme(), Writers());

        pages[0].Downloads.Select(download => download.FileName).ShouldBe(["Ann_Example_CV_EN_ATS.txt", "Ann_Example_CV_EN.md", "Ann_Example_CV_EN.json"]);
    }

    [Fact]
    public async Task RendersEachPageInItsLanguageWithThemeServicesGivenPages()
    {
        using var folder = new TempFolder();

        await Sites.BuildAsync(folder, new FakeTheme(), Writers());

        var hungarian = folder.Read("dist/hu/index.html");
        hungarian.ShouldStartWith("<!DOCTYPE html><html lang=\"hu\">");
        hungarian.ShouldContain("<p id=\"greeting\">rendered by the fake theme</p>");
        hungarian.ShouldContain("<p id=\"uri\">https://cv.example.com/hu/</p>");
        hungarian.ShouldContain("<p id=\"culture\">hu-HU</p>");
    }

    // The PDF draws the address as an image; any other format would carry it as text.
    [Fact]
    public async Task HandsEmailToPdfWriterOnlyGivenAddress()
    {
        using var folder = new TempFolder();
        var pdf = new RecordingWriter(DownloadFormat.Pdf);
        var docx = new RecordingWriter(DownloadFormat.Docx);

        await Sites.BuildAsync(folder, new FakeTheme(), Writers(pdf, docx), email: "ann@example.com");

        pdf.Contexts.ShouldNotBeEmpty();
        pdf.Contexts.ShouldAllBe(context => context.ContactEmail == "ann@example.com");
        docx.Contexts.ShouldAllBe(context => context.ContactEmail == null);
    }

    [Fact]
    public async Task TailorsFocusDownloadsGivenFocusProfiles()
    {
        using var folder = new TempFolder();
        var pdf = new RecordingWriter(DownloadFormat.Pdf);

        await Sites.BuildAsync(folder, new FakeTheme(), Writers(pdf));

        var tailored = pdf.Contexts.Single(context => context.Download.FileName == "Ann_Example_CV_HU_Architect.pdf");
        tailored.Focus!.Profile.Id.ShouldBe("architect");
        tailored.PageUrl.ShouldBe(new Uri("https://cv.example.com/hu/"));
        tailored.Source.ShouldNotBeNull();
    }
}
