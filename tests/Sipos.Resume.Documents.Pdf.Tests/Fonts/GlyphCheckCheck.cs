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
    public void FindsNothingGivenAccentedLatinCv() => GlyphCheck.Check(Edition(Samples.Accented), theme: null).ShouldBeEmpty();

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
        var issue = GlyphCheck.Check(Edition(Samples.English.Replace("\"Hungarian\"", "\"日本語 ★\"", StringComparison.Ordinal)), theme: null).ShouldHaveSingleItem();

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

        GlyphCheck.Check(Edition(json), theme: null).Select(issue => issue.Path).ShouldBe(["/work/0/x-keywords/0", "/projects/1/keywords/0"]);
    }

    // The aliases help match job ads and are never drawn, so another script there is fine.
    [Fact]
    public void IgnoresAliasesGivenOtherScript() =>
        GlyphCheck.Check(Edition(Samples.English.Replace("\"csharp\"", "\"シーシャープ\"", StringComparison.Ordinal)), theme: null).ShouldBeEmpty();

    // A theme with fonts of its own is checked by QuestPDF while drawing, not against Plex.
    [Fact]
    public void ChecksNothingGivenThemeWithOwnFonts()
    {
        var theme = DocumentTheme.Neutral with { SansFamily = "Lato" };

        GlyphCheck.Check(Edition(Samples.English.Replace("\"Hungarian\"", "\"日本語\"", StringComparison.Ordinal)), theme).ShouldBeEmpty();
    }
}
