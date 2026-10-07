using System.Text;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Mapping;
using Sipos.Resume.Documents.Pdf.Fonts;

namespace Sipos.Resume.Documents.Pdf.Tests.Fonts;

public class GlyphCheckCheck
{
    private static ResumeEdition Edition(string json)
    {
        var resume = Samples.Read(json);
        return new ResumeEdition("resume.en.json", resume, ResumeMapper.Map(resume, LanguageCatalog.Describe("en", null, isDefault: true)));
    }

    // Plex Sans draws Latin with every accent the four languages use, Greek, Cyrillic and arrows; no emoji, stars or CJK.
    [Theory]
    [InlineData('A', true)]
    [InlineData('ő', true)]
    [InlineData('đ', true)]
    [InlineData('Ж', true)]
    [InlineData('α', true)]
    [InlineData('→', true)]
    [InlineData('★', false)]
    [InlineData('日', false)]
    public void KnowsGlyphsGivenCharacter(char character, bool covered) => PlexFonts.Covers(character).ShouldBe(covered);

    [Fact]
    public void KnowsMonoHasNoGreekGivenTechnology() => PlexFonts.Covers('α', mono: true).ShouldBeFalse();

    [Fact]
    public void KnowsNoEmojiGivenSupplementaryPlane() => PlexFonts.Covers(new Rune(0x1F600).Value).ShouldBeFalse();

    [Fact]
    public void FindsNothingGivenAccentedLatinCv() => GlyphCheck.Check(Edition(Samples.Accented), theme: null, contactEmail: "ann@example.com").ShouldBeEmpty();

    [Fact]
    public void PointsToContactEmailGivenCharacterThePdfCannotDraw()
    {
        var issue = GlyphCheck.Check(Edition(Samples.English), theme: null, "ann+★@example.com").ShouldHaveSingleItem();

        issue.Source.ShouldBe("RESUME_CONTACT_EMAIL");
        issue.Path.ShouldBe("");
        issue.Message.ShouldContain("U+2605 (★)");
        issue.Message.ShouldNotContain("ann+");
    }

    [Fact]
    public void ChecksOnlyShareImageTextGivenMissingGlyphElsewhere()
    {
        var edition = Edition(Samples.English.Replace("\"Hungarian\"", "\"日本語\"", StringComparison.Ordinal));

        GlyphCheck.CheckShareImage(edition, theme: null, new Uri("https://cv.example.com/")).ShouldBeEmpty();
        GlyphCheck.CheckShareImage(Edition(Samples.English.Replace("\"Ann Example\"", "\"日本語\"", StringComparison.Ordinal)),
            theme: null,
            new Uri("https://cv.example.com/")).Select(issue => issue.Path).ShouldBe(["/basics/name"]);
    }

    // QuestPDF has no fallback font and would fail while drawing; the check names the field and the character first.
    [Fact]
    public void PointsToFieldGivenCharacterThePdfCannotDraw()
    {
        var issue = GlyphCheck.Check(Edition(Samples.English.Replace("\"Hungarian\"", "\"日本語 ★\"", StringComparison.Ordinal)), theme: null, contactEmail: null).ShouldHaveSingleItem();

        issue.Path.ShouldBe("/languages/0/language");
        issue.Message.ShouldContain("U+65E5 (日)");
        issue.Message.ShouldContain("U+2605 (★)");
    }

    // A position's or a project's technologies are set in Plex Mono, which lacks Greek; the profile is in Sans.
    [Fact]
    public void ChecksTechnologiesAgainstMonoGivenGreekLetters()
    {
        var json = Samples.English
            .Replace("\"Rust\"", "\"λ-calculus\"", StringComparison.Ordinal)
            .Replace("\"x-focus\": [\"architect\"] }", "\"x-focus\": [\"architect\"], \"x-keywords\": [\"λ\"] }", StringComparison.Ordinal)
            .Replace("Builds .NET systems.", "Builds .NET systems in Ελλάδα.", StringComparison.Ordinal);

        GlyphCheck.Check(Edition(json), theme: null, contactEmail: null).Select(issue => issue.Path).ShouldBe(["/work/0/x-keywords/0", "/projects/1/keywords/0"]);
    }

    // The aliases help match job ads and are never drawn, so another script there is fine.
    [Fact]
    public void IgnoresAliasesGivenOtherScript() =>
        GlyphCheck.Check(Edition(Samples.English.Replace("\"csharp\"", "\"シーシャープ\"", StringComparison.Ordinal)), theme: null, contactEmail: null).ShouldBeEmpty();

    // A designed theme's custom Sans cannot bypass the ATS layout's fixed Plex Sans.
    [Fact]
    public void ChecksAtsSansGivenThemeWithOwnFonts()
    {
        var theme = DocumentTheme.Neutral with { SansFamily = "Lato" };

        GlyphCheck.Check(Edition(Samples.English.Replace("\"Hungarian\"", "\"日本語\"", StringComparison.Ordinal)), theme, contactEmail: null)
            .Select(issue => issue.Path).ShouldBe(["/languages/0/language"]);
    }

    [Theory]
    [InlineData("image")]
    [InlineData("url")]
    public void IgnoresUnrenderedBasicsTargetGivenUnsupportedCharacter(string field)
    {
        var json = field == "image"
            ? Samples.English.Replace("\"phone\":", "\"image\": \"https://example.com/😀\", \"phone\":", StringComparison.Ordinal)
            : Samples.English.Replace("https://cv.example.com/", "https://example.com/😀", StringComparison.Ordinal);
        GlyphCheck.Check(Edition(json), theme: null, contactEmail: null).ShouldBeEmpty();
    }

    [Fact]
    public void ChecksDisplayedProfileAddressAsRendered()
    {
        var json = Samples.English.Replace("https://github.com/ann", "https://github.com/😀", StringComparison.Ordinal);
        GlyphCheck.Check(Edition(json), theme: null, contactEmail: null).ShouldBeEmpty();
    }

    [Fact]
    public void IgnoresUnrenderedContactAndProjectTargets()
    {
        var json = Samples.English.Replace("https://example.com/contact", "https://example.com/😀", StringComparison.Ordinal)
            .Replace("\"entity\": \"Globex\"", "\"url\": \"https://example.com/😀\", \"entity\": \"Globex\"", StringComparison.Ordinal);
        GlyphCheck.Check(Edition(json), theme: null, contactEmail: null).ShouldBeEmpty();
    }

    [Fact]
    public void IgnoresLocationComponentsGivenRenderedOverride()
    {
        var json = Samples.English.Replace("\"region\": \"Europe\"", "\"region\": \"日本\"", StringComparison.Ordinal);
        GlyphCheck.Check(Edition(json), theme: null, contactEmail: null).ShouldBeEmpty();
    }

    [Fact]
    public void ChecksSansGivenRegisteredNonDefaultMonoFamily()
    {
        var theme = DocumentTheme.Neutral with { MonoFamily = PlexFonts.Sans };
        PlexFonts.Resolve(theme).MonoFamily.ShouldBe(PlexFonts.Sans);
        var edition = Edition(Samples.English.Replace("\"Hungarian\"", "\"日本語\"", StringComparison.Ordinal));

        GlyphCheck.Check(edition, theme, contactEmail: null).Select(issue => issue.Path).ShouldBe(["/languages/0/language"]);
    }

    [Fact]
    public void ChecksMonoGivenRegisteredNonDefaultSansFamily()
    {
        var theme = DocumentTheme.Neutral with { SansFamily = PlexFonts.Mono };
        PlexFonts.Resolve(theme).SansFamily.ShouldBe(PlexFonts.Mono);
        var edition = Edition(Samples.English.Replace("\"Rust\"", "\"λ-calculus\"", StringComparison.Ordinal));

        GlyphCheck.Check(edition, theme, contactEmail: null).Select(issue => issue.Path).ShouldBe(["/projects/1/keywords/0"]);
    }

    [Fact]
    public void AcceptsGreekTechnologyGivenMonoFamilyThatSupportsIt()
    {
        var theme = DocumentTheme.Neutral with { MonoFamily = PlexFonts.Sans };
        var edition = Edition(Samples.English.Replace("\"Rust\"", "\"λ-calculus\"", StringComparison.Ordinal));

        GlyphCheck.Check(edition, theme, contactEmail: null).ShouldBeEmpty();
    }

    [Fact]
    public void ChecksActualFamilyGivenPlexMonoUsedForDesignedSansText()
    {
        var theme = DocumentTheme.Neutral with { SansFamily = PlexFonts.Mono };
        var edition = Edition(Samples.English.Replace("Software Architect", "λ-calculus", StringComparison.Ordinal));

        GlyphCheck.Check(edition, theme, contactEmail: null).Select(issue => issue.Path).ShouldBe(["/basics/label"]);
    }
}
