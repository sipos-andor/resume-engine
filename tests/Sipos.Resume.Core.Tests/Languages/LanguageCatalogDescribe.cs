using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Languages;

namespace Sipos.Resume.Core.Tests.Languages;

public class LanguageCatalogDescribe
{
    [Theory]
    [InlineData("resume.en.json", "en")]
    [InlineData("content/resume.sr-Latn.json", "sr-Latn")]
    [InlineData("resume.json", null)]
    [InlineData("site.json", null)]
    [InlineData("resume.hu.md", null)]
    public void ReadsTagFromFileName(string file, string? tag) => LanguageCatalog.TagOf(file).ShouldBe(tag);

    [Theory]
    [InlineData("hu", "hu-HU", "hu", "Magyar", "hu_HU")]
    [InlineData("hr", "hr-HR", "hr", "Hrvatski", "hr_HR")]
    [InlineData("sr-Latn", "sr-Latn-RS", "sr", "Srpski (latinica)", "sr_RS")]
    public void DerivesCultureAndPathGivenTag(string tag, string culture, string segment, string endonym, string locale)
    {
        var language = LanguageCatalog.Describe(tag, meta: null, isDefault: false);

        language.Culture.Name.ShouldBe(culture);
        language.PathSegment.ShouldBe(segment);
        language.HomePath.ShouldBe($"/{segment}/");
        language.Endonym.ShouldBe(endonym);
        language.OgLocale.ShouldBe(locale);
    }

    // Downloads are named after the path segment, which is unique per site, and the default language after its tag.
    [Theory]
    [InlineData("en", null, true, "EN")]
    [InlineData("sr-Latn", null, false, "SR")]
    [InlineData("en-GB", "en-gb", false, "EN-GB")]
    public void NamesDownloadsAfterPathGivenLanguage(string tag, string? path, bool isDefault, string code) =>
        LanguageCatalog.Describe(tag, path is null ? null : new JsonResumeMeta { Path = path }, isDefault).FileCode.ShouldBe(code);

    [Fact]
    public void TakesOverridesGivenMeta()
    {
        var language = LanguageCatalog.Describe("sr-Latn", new JsonResumeMeta { Endonym = "Srpski", OgLocale = "sr_RS", Path = "srb" }, isDefault: false);

        language.Endonym.ShouldBe("Srpski");
        language.HomePath.ShouldBe("/srb/");
    }

    [Fact]
    public void ServesRootGivenDefaultLanguage()
    {
        var language = LanguageCatalog.Describe("en", new JsonResumeMeta { OgLocale = "en_GB" }, isDefault: true);

        language.HomePath.ShouldBe("/");
        language.OgLocale.ShouldBe("en_GB");
        language.FileCode.ShouldBe("EN");
    }

    [Fact]
    public void OrdersDefaultFirstThenGivenOrderThenTag()
    {
        var languages = new[] { "sr-Latn", "hr", "de", "hu", "en" }.Select(tag => LanguageCatalog.Describe(tag, null, tag == "en"));

        LanguageCatalog.Order(languages, ["hu", "hr", "sr-Latn"]).Select(language => language.Tag).ShouldBe(["en", "hu", "hr", "sr-Latn", "de"]);
    }
}
