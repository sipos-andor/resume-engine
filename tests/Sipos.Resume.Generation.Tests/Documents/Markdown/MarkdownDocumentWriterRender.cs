using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Model;
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
    public void KeepsTitleAsParagraphGivenLeadingBlockMarkers()
    {
        var document = SampleDocuments.English();
        var person = document.Person with { Title = "# Architect", Tagline = "1. Builder" };
        var context = Contexts.For(document with { Person = person }, DownloadFormat.Markdown, DocumentVariant.Designed);

        var html = Markdig.Markdown.ToHtml(MarkdownDocumentWriter.Render(context));

        html.ShouldContain("<p># Architect · 1. Builder</p>");
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
    public void KeepsHtmlEntitiesLiteralGivenMarkdownText()
    {
        var html = Markdig.Markdown.ToHtml(MarkdownText.Escape("&copy; &#35;"));

        html.ShouldContain("&amp;copy; &amp;#35;");
    }

    // A line break in a value would end the list item or the paragraph it is written into.
    [Fact]
    public void JoinsLinesGivenTextWithLineBreaks() => MarkdownText.Escape("one\r\ntwo\rthree\nfour").ShouldBe("one two three four");

    // Only where the text starts a block: there "2023. March" would become a list starting at 2023.
    [Theory]
    [InlineData("2023. March", "2023\\. March")]
    [InlineData("1) First", "1\\) First")]
    [InlineData("2023.", "2023\\.")]
    [InlineData("# One", "\\# One")]
    [InlineData("- One", "\\- One")]
    [InlineData("1.5 years", "1.5 years")]
    [InlineData("2023", "2023")]
    public void KeepsTextGivenBlockThatStartsLikeListOrHeading(string text, string escaped) => MarkdownText.EscapeBlock(text).ShouldBe(escaped);

    // A space or a parenthesis would end the link's destination early.
    [Fact]
    public void EncodesSpacesAndParenthesesGivenLinkDestination() =>
        MarkdownText.Link("https://example.com/a b/(c)").ShouldBe("[example.com/a b/(c)](https://example.com/a%20b/%28c%29)");

    [Fact]
    public void KeepsDateAsWrittenGivenItInsideLine() => MarkdownText.Escape("2023. March – 2023. May").ShouldBe("2023. March – 2023. May");

    [Fact]
    public void WritesHeadingsInCvLanguageGivenHungarianCv()
    {
        var markdown = MarkdownDocumentWriter.Render(Contexts.For(SampleDocuments.Hungarian(), DownloadFormat.Markdown, DocumentVariant.Designed));

        markdown.ShouldContain("## Szakmai tapasztalat");
        markdown.ShouldContain("2023. március – 2023. május");
    }

    [Fact]
    public void WritesAvailabilityDateGivenFromStatus()
    {
        var english = SampleDocuments.English();
        var person = english.Person with
        {
            Availability = new Availability(AvailabilityStatus.From, PartialDate.Parse("2027-05"), "https://example.com/capacity", "Available from"),
        };

        MarkdownDocumentWriter.Render(Contexts.For(english with { Person = person }, DownloadFormat.Markdown, DocumentVariant.Designed))
            .ShouldContain("Available from (May 2027)");
    }
}
