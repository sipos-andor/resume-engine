using System.Text;
using Sipos.Resume.Core.Content;

namespace Sipos.Resume.Core.Tests.Content;

public class ContentLoaderLoad
{
    private const string Site = """
        {
          "origin": "https://cv.example.com",
          "defaultLanguage": "en",
          "languageOrder": ["hu"],
          "downloadPrefix": "Ann_Example_CV",
          "themeStorageKey": "cv-theme",
          "languageStorageKey": "cv-language"
        }
        """;

    [Fact]
    public void ReturnsSetWithDefaultLanguageFirstGivenValidContent()
    {
        var result = ContentLoader.Load([File("resume.hu.json", Samples.Hungarian), File("resume.en.json", Samples.English), File("site.json", Site)], analyticsToken: " token ");

        result.Issues.ShouldBeEmpty();
        var set = result.Set.ShouldNotBeNull();
        set.Languages.Select(language => language.HomePath).ShouldBe(["/", "/hu/"]);
        set.Editions[1].FileName.ShouldBe("resume.hu.json");
        set.Settings.Origin.ShouldBe(new Uri("https://cv.example.com"));
        set.Settings.DefaultLanguage.ShouldBe("en");
        set.Settings.AnalyticsToken.ShouldBe("token");
    }

    [Fact]
    public void LeavesOutAnalyticsGivenNoToken() =>
        ContentLoader.Load([File("resume.en.json", Samples.English), File("site.json", Site)], analyticsToken: "").Set!.Settings.AnalyticsToken.ShouldBeNull();

    [Fact]
    public void IgnoresOtherFilesGivenFolderWithReadme() =>
        ContentLoader.Load([File("resume.en.json", Samples.English), File("site.json", Site), File("README.md", "# Content")], null).Issues.ShouldBeEmpty();

    // Every issue of every file in one pass, so a translator needs one run, not one per mistake.
    [Fact]
    public void ReportsEveryIssueAndNoSetGivenSeveralProblems()
    {
        var hungarian = Samples.Hungarian.Replace("\"startDate\": \"2015-03\"", "\"startDate\": \"2016-03\"", StringComparison.Ordinal);
        var site = Site.Replace("Ann_Example_CV", "Ann Example CV", StringComparison.Ordinal);

        var result = ContentLoader.Load([File("resume.en.json", Samples.English), File("resume.hu.json", hungarian), File("site.json", site)], null);

        result.Set.ShouldBeNull();
        result.Issues.ShouldContain(issue => issue.Source == "site.json" && issue.Path == "/downloadPrefix");
        result.Issues.ShouldContain(issue => issue.Source == "resume.hu.json" && issue.Path.EndsWith("/startDate", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadsSiteFileGivenByteOrderMark()
    {
        var result = ContentLoader.Load([File("resume.en.json", Samples.English), new ContentFile("site.json", (byte[])[.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Site)])], null);

        result.Issues.ShouldBeEmpty();
    }

    // Language tags ignore case, so these are one language twice.
    [Fact]
    public void ReportsSecondFileGivenSameLanguageInOtherCase()
    {
        var issue = ContentLoader.Load([File("resume.en.json", Samples.English), File("resume.EN.json", Samples.English), File("site.json", Site)], null)
            .Issues.ShouldHaveSingleItem();

        issue.Source.ShouldBe("resume.en.json");
        issue.Message.ShouldContain("resume.EN.json");
    }

    [Fact]
    public void ReportsMissingSiteFileGivenNoSiteJson() =>
        ContentLoader.Load([File("resume.en.json", Samples.English)], null).Issues.ShouldContain(issue => issue.Source == "site.json" && issue.Path == "");

    [Fact]
    public void ReportsMissingDefaultLanguageGivenNoFileForIt()
    {
        var result = ContentLoader.Load([File("resume.hu.json", Samples.Hungarian), File("site.json", Site)], null);

        result.Issues.ShouldContain(issue => issue.Source == "site.json" && issue.Path == "/defaultLanguage");
    }

    [Theory]
    [InlineData("https://cv.example.com/cv")]
    [InlineData("cv.example.com")]
    [InlineData("ftp://cv.example.com")]
    public void ReportsOriginGivenPathOrNoSchemeOrOtherScheme(string origin)
    {
        var site = Site.Replace("https://cv.example.com", origin, StringComparison.Ordinal);

        ContentLoader.Load([File("resume.en.json", Samples.English), File("site.json", site)], null).Issues.ShouldContain(issue => issue.Path == "/origin");
    }

    [Fact]
    public void ReportsNameGivenContentFileWithoutLanguageTag() =>
        ContentLoader.Load([File("resume.en.json", Samples.English), File("resume.english.json", Samples.English), File("site.json", Site)], null)
            .Issues.ShouldContain(issue => issue.Source == "resume.english.json");

    // A language the engine has no headings for would fall back to English unnoticed.
    [Fact]
    public void ReportsMissingLabelsGivenLanguageWithoutThem()
    {
        var result = ContentLoader.Load([File("resume.en.json", Samples.English), File("resume.de.json", Samples.Hungarian), File("site.json", Site)], null);

        result.Issues.ShouldContain(issue => issue.Source == "resume.de.json" && issue.Message.Contains("labels", StringComparison.Ordinal));
    }

    [Fact]
    public void ReportsEmailGivenAddressInContent()
    {
        var english = Samples.English.Replace("Builds .NET systems.", "Write to ann@example.com.", StringComparison.Ordinal);

        ContentLoader.Load([File("resume.en.json", english), File("site.json", Site)], null).Issues.ShouldContain(issue => issue.Path == "/basics/summary");
    }

    private static ContentFile File(string name, string text) => new(name, Encoding.UTF8.GetBytes(text));
}
