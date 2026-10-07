using System.Text;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Generation.Documents.PlainText;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Documents.PlainText;

public class PlainTextDocumentWriterRender
{
    private static string English() => PlainTextDocumentWriter.Render(Contexts.For(SampleDocuments.English(), DownloadFormat.PlainText, DocumentVariant.Ats));

    [Fact]
    public void WritesStandardHeadingsInCapitalsGivenAtsLayout()
    {
        var lines = English().Split('\n');

        lines.ShouldContain("SUMMARY");
        lines.ShouldContain("SKILLS");
        lines.ShouldContain("WORK EXPERIENCE");
        lines.ShouldContain("CERTIFICATIONS");
    }

    [Fact]
    public void WritesNumericDatesAndFullUrlsGivenPosition()
    {
        var text = English();

        text.ShouldContain("acme | 11/2022 – present");
        text.ShouldContain("Online CV: https://cv.example.com/");
        text.ShouldContain("Contact: Get in touch – https://example.com/contact");
    }

    // Without a role the organization is the heading; its note must come with it, as in the designed documents.
    [Fact]
    public void KeepsNoteGivenPositionWithoutRole()
    {
        var english = SampleDocuments.English();
        var document = english with { Positions = [english.Positions[0] with { Role = null, Note = "formerly a sole proprietorship" }] };

        PlainTextDocumentWriter.Render(Contexts.For(document, DownloadFormat.PlainText, DocumentVariant.Ats)).ShouldContain("acme (formerly a sole proprietorship)\n");
    }

    [Fact]
    public void WritesLevelsInWordsGivenSkills() => English().ShouldContain(".NET platform: C# (Expert), LINQ (Expert), Blazor (Proficient)");

    [Fact]
    public void ListsStrengthsUnderSummaryGivenStrengths() => English().ShouldContain("Builds .NET systems.\n- Full-stack delivery: End to end.\n");

    // Without the mark older Windows tools read the file as ANSI and garble accented names.
    [Fact]
    public void StartsWithByteOrderMarkGivenAnyCv()
    {
        using var output = new MemoryStream();
        new PlainTextDocumentWriter().Write(Contexts.For(SampleDocuments.English(), DownloadFormat.PlainText, DocumentVariant.Ats), output);

        output.ToArray().Take(3).ShouldBe(Encoding.UTF8.GetPreamble());
    }

    [Fact]
    public void SupportsOnlyAtsLayoutGivenVariants()
    {
        new PlainTextDocumentWriter().Supports(DocumentVariant.Ats).ShouldBeTrue();
        new PlainTextDocumentWriter().Supports(DocumentVariant.Designed).ShouldBeFalse();
    }
}
