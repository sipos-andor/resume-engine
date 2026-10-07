using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Generation.Documents.Markdown;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Documents.Markdown;

public class MarkdownDocumentWriterRender
{
    private static string English(string? focus = null) =>
        MarkdownDocumentWriter.Render(Contexts.For(SampleDocuments.English(), DownloadFormat.Markdown, DocumentVariant.Designed, focus));

    [Fact]
    public void StartsWithNameAndTitleGivenCv()
    {
        var markdown = English();

        markdown.ShouldStartWith("# Ann Example\n\nSoftware Architect\n");
    }

    [Fact]
    public void WritesSectionsInOutlineOrderGivenDesignedLayout()
    {
        var headings = English().Split('\n').Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToList();

        headings.ShouldBe(["## Profile", "## Selected strengths", "## Professional experience", "## Projects", "## Technologies and competences", "## Education, certification and training", "## Awards and competitions", "## Languages"]);
    }

    // C# must not turn into a heading, and the phone and pages must stay clickable.
    [Fact]
    public void KeepsTechnologyNamesAndLinksGivenMarkupCharacters()
    {
        var html = Markdig.Markdown.ToHtml(English());

        html.ShouldContain("C#");
        html.ShouldContain("href=\"tel:+36301234567\"");
        html.ShouldContain("href=\"https://cv.example.com/\"");
        html.ShouldContain(">cv.example.com<");
        html.ShouldContain("href=\"https://example.com/contact\"");
    }

    [Fact]
    public void NestsClientProjectsUnderTheirPositionGivenProjectsOfPosition()
    {
        var markdown = English();

        markdown.IndexOf("### Founder – acme", StringComparison.Ordinal).ShouldBeLessThan(markdown.IndexOf("##### Portal – Globex", StringComparison.Ordinal));
        markdown.IndexOf("##### Portal – Globex", StringComparison.Ordinal).ShouldBeLessThan(markdown.IndexOf("### Engineer – Initech", StringComparison.Ordinal));
        markdown.ShouldContain("*Lead · March 2023 – May 2023*");
        markdown.ShouldContain("**Technologies:** C#, Azure");
    }

    [Fact]
    public void NamesFocusGivenTailoredDocument() => English("architect").ShouldContain("**Focus:** Software architect");

    [Fact]
    public void EscapesMarkupGivenTextWithAsterisks() => MarkdownText.Escape("*bold* [x] <b>").ShouldBe("\\*bold\\* \\[x\\] \\<b\\>");

    [Fact]
    public void WritesHeadingsInCvLanguageGivenHungarianCv()
    {
        var markdown = MarkdownDocumentWriter.Render(Contexts.For(SampleDocuments.Hungarian(), DownloadFormat.Markdown, DocumentVariant.Designed));

        markdown.ShouldContain("## Szakmai tapasztalat");
        markdown.ShouldContain("2023. március – 2023. május");
    }
}
