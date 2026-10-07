using Sipos.Resume.Generation.Seo;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Seo;

public class LlmsTxtWriterWrite
{
    [Fact]
    public void FollowsLlmsTxtShapeGivenDefaultPage()
    {
        var pages = SamplePages.Sample();

        var text = LlmsTxtWriter.Write(pages[0], pages);

        text.ShouldStartWith("# Ann Example\n\n> Software Architect\n\nBuilds .NET systems.\n\n## CV\n\n");
        text.ShouldContain("- [Ann Example – CV (Markdown)](https://cv.example.com/index.md)");
        text.ShouldContain("- [JSON Resume](https://cv.example.com/resume.json)");
        text.ShouldContain("(https://cv.example.com/llms-full.txt)");
        text.ShouldContain("- [PDF · ATS](https://cv.example.com/downloads/Ann_Example_CV_EN_ATS.pdf)");
        text.ShouldContain("- [PDF · Software architect](https://cv.example.com/downloads/Ann_Example_CV_EN_Architect.pdf)");
        text.ShouldContain("- [Get in touch](https://example.com/contact)");
        text.ShouldContain("## This CV in other languages\n\n- [Magyar](https://cv.example.com/hu/llms.txt)");
    }

    [Fact]
    public void WritesHeadingsInPageLanguageGivenOtherLanguage()
    {
        var pages = SamplePages.Sample();

        var text = LlmsTxtWriter.Write(pages[1], pages);

        text.ShouldContain("## Önéletrajz");
        text.ShouldContain("## Letöltések");
        text.ShouldContain("- [Example Ann – Önéletrajz (Markdown)](https://cv.example.com/hu/index.md)");
        text.ShouldContain("(https://cv.example.com/llms.txt)");
        text.ShouldNotContain("llms-full.txt");
    }

    // The quote and the summary each start a block, where "1." or "#" would make a list or a heading.
    [Fact]
    public void KeepsTitleAndSummaryAsTextGivenListOrHeadingMarkers()
    {
        var pages = SamplePages.Sample();
        var document = pages[0].Document with { Person = pages[0].Document.Person with { Title = "# Architect", Tagline = null, Summary = "1. Builds systems." } };

        var text = LlmsTxtWriter.Write(pages[0] with { Document = document }, pages);

        text.ShouldContain("> \\# Architect\n");
        text.ShouldContain("\n1\\. Builds systems.\n");
    }

    [Fact]
    public void SeparatesLanguagesWithRulesGivenFullText() => LlmsTxtWriter.WriteFull(["# A\n", "# B\n"]).ShouldBe("# A\n\n---\n\n# B\n");
}
