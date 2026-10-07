using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Tests.Validation;

public class ParityValidatorCompare
{
    [Theory]
    [InlineData("volunteer", "organization")]
    [InlineData("volunteer", "startDate")]
    [InlineData("volunteer", "endDate")]
    [InlineData("volunteer", "url")]
    [InlineData("publications", "publisher")]
    [InlineData("publications", "releaseDate")]
    [InlineData("publications", "url")]
    [InlineData("references", "name")]
    public void ReportsUnmodeledFactGivenChangedOrMissingValue(string section, string field)
    {
        var original = Samples.Read($"{{\"{section}\":[{{\"{field}\":\"original\"}}]}}");
        foreach (var translated in new[]
        {
            Samples.Read($"{{\"{section}\":[{{\"{field}\":\"changed\"}}]}}"),
            Samples.Read($"{{\"{section}\":[{{}}]}}"),
        })
        {
            ParityValidator.Compare(("en", original), [("hu", translated)])
                .Select(issue => issue.Path).ShouldBe([$"/{section}/0/{field}"]);
            ParityValidator.Compare(("hu", translated), [("en", original)])
                .Select(issue => issue.Path).ShouldBe([$"/{section}/0/{field}"]);
        }
    }

    [Theory]
    [InlineData("volunteer")]
    [InlineData("publications")]
    [InlineData("interests")]
    [InlineData("references")]
    public void ReportsCountGivenDroppedUnmodeledSection(string section)
    {
        var original = Samples.Read($"{{\"{section}\":[{{}}]}}");
        var translated = Samples.Read("{}");

        ParityValidator.Compare(("en", original), [("hu", translated)])
            .Select(issue => issue.Path).ShouldContain($"/{section}");
    }

    [Theory]
    [InlineData("volunteer", "highlights")]
    [InlineData("interests", "keywords")]
    public void ComparesCountsGivenTranslatedUnmodeledLists(string section, string field)
    {
        var original = Samples.Read($"{{\"{section}\":[{{\"name\":\"English\",\"{field}\":[\"English\"]}}]}}");
        var translated = Samples.Read($"{{\"{section}\":[{{\"name\":\"Magyar\",\"{field}\":[\"Magyar\"]}}]}}");
        ParityValidator.Compare(("en", original), [("hu", translated)]).ShouldBeEmpty();

        translated = Samples.Read($"{{\"{section}\":[{{\"{field}\":[]}}]}}");
        ParityValidator.Compare(("en", original), [("hu", translated)])
            .Select(issue => issue.Path).ShouldBe([$"/{section}/0/{field}"]);
    }

    // Texts are translated, facts are not: names, titles and labels may differ.
    [Fact]
    public void FindsNothingGivenTranslatedTextsOnly() =>
        ParityValidator.Compare(("resume.en.json", Samples.Read(Samples.English)), [("resume.hu.json", Samples.Read(Samples.Hungarian))]).ShouldBeEmpty();

    [Theory]
    [InlineData("countryCode", "GB", "HU")]
    [InlineData("countryCode", "GB", null)]
    [InlineData("countryCode", null, "HU")]
    [InlineData("postalCode", "SW1A 1AA", "1011")]
    [InlineData("postalCode", "SW1A 1AA", null)]
    [InlineData("postalCode", null, "1011")]
    public void ReportsLocationFactGivenDifferentOrMissingCode(string field, string? originalCode, string? translatedCode)
    {
        var original = Samples.Read(Samples.English);
        var location = field == "countryCode"
            ? new JsonResumeLocation { CountryCode = originalCode }
            : new JsonResumeLocation { PostalCode = originalCode };
        original = original with { Basics = original.Basics! with { Location = location } };
        var translatedLocation = field == "countryCode"
            ? location with { CountryCode = translatedCode }
            : location with { PostalCode = translatedCode };
        var translated = original with { Basics = original.Basics with { Location = translatedLocation } };

        var issue = ParityValidator.Compare(("resume.en.json", original), [("resume.hu.json", translated)]).Single();

        issue.Source.ShouldBe("resume.hu.json");
        issue.Path.ShouldBe($"/basics/location/{field}");
    }

    [Fact]
    public void AcceptsLocationGivenMatchingCodesAndTranslatedTexts()
    {
        var original = Samples.Read(Samples.English);
        var location = new JsonResumeLocation { CountryCode = "GB", PostalCode = "SW1A 1AA", City = "London", Region = "England" };
        original = original with { Basics = original.Basics! with { Location = location } };
        var translated = original with
        {
            Basics = original.Basics with { Location = location with { City = "London", Region = "Anglia" } },
        };

        ParityValidator.Compare(("resume.en.json", original), [("resume.hu.json", translated)]).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, null, "resume.hu.json")]
    [InlineData("portal", null, "resume.hu.json")]
    [InlineData(null, "portal", "resume.en.json")]
    [InlineData("", "", "resume.hu.json")]
    public void RequiresExplicitProjectIdGivenTranslatedName(string? originalId, string? translatedId, string source)
    {
        var original = Samples.Read(Samples.English) with
        {
            Projects = [new JsonResumeProject { Name = "Internal portal", Id = originalId }],
        };
        var translated = original with
        {
            Projects = [original.Projects[0] with { Name = "Belső portál", Id = translatedId }],
        };

        var issue = ParityValidator.Compare(("resume.en.json", original), [("resume.hu.json", translated)]).Single();

        issue.Source.ShouldBe(source);
        issue.Path.ShouldBe("/projects/0/x-id");
        issue.Message.ShouldContain("same explicit x-id in every language");
    }

    [Theory]
    [InlineData("Internal portal", null, null)]
    [InlineData("Belső portál", "portal", "portal")]
    public void AcceptsProjectGivenSameNameOrExplicitId(string name, string? originalId, string? translatedId)
    {
        var original = Samples.Read(Samples.English) with
        {
            Projects = [new JsonResumeProject { Name = "Internal portal", Id = originalId }],
        };
        var translated = original with
        {
            Projects = [original.Projects[0] with { Name = name, Id = translatedId }],
        };

        ParityValidator.Compare(("resume.en.json", original), [("resume.hu.json", translated)]).ShouldBeEmpty();
    }

    [Fact]
    public void ReportsProjectIdGivenDifferentExplicitIds()
    {
        var original = Samples.Read(Samples.English) with
        {
            Projects = [new JsonResumeProject { Name = "Internal portal", Id = "portal" }],
        };
        var translated = original with
        {
            Projects = [original.Projects[0] with { Name = "Belső portál", Id = "other" }],
        };

        ParityValidator.Compare(("resume.en.json", original), [("resume.hu.json", translated)])
            .Select(issue => issue.Path).ShouldBe(["/projects/0/x-id"]);
    }

    [Fact]
    public void ReportsDateGivenDifferentStartDate()
    {
        var hungarian = Samples.Read(Samples.Hungarian);
        var work = hungarian.Work.ToList();
        work[1] = work[1] with { StartDate = "2015-04" };

        var issue = ParityValidator.Compare(("resume.en.json", Samples.Read(Samples.English)), [("resume.hu.json", hungarian with { Work = work })]).Single();

        issue.Source.ShouldBe("resume.hu.json");
        issue.Path.ShouldBe("/work/1/startDate");
        issue.Message.ShouldContain("2015-03");
    }

    [Fact]
    public void ReportsKeywordsGivenTranslatedTechnology()
    {
        var hungarian = Samples.Read(Samples.Hungarian);
        var projects = hungarian.Projects.ToList();
        projects[0] = projects[0] with { Keywords = ["C#", "Azure felhő"] };

        ParityValidator.Compare(("resume.en.json", Samples.Read(Samples.English)), [("resume.hu.json", hungarian with { Projects = projects })])
            .Select(issue => issue.Path).ShouldBe(["/projects/0/keywords"]);
    }

    [Fact]
    public void PreservesKeywordBoundariesGivenDelimiterInKeyword()
    {
        var source = Samples.Read(Samples.English);
        var original = source with { Skills = [source.Skills[0] with { Keywords = ["C# | Azure"] }] };
        var translated = original with { Skills = [original.Skills[0] with { Keywords = ["C#", "Azure"] }] };

        ParityValidator.Compare(("resume.en.json", original), [("resume.hu.json", translated)])
            .Select(issue => issue.Path).ShouldBe(["/skills/0/keywords"]);
    }

    [Fact]
    public void ReportsCountAndMissingItemGivenDroppedPosition()
    {
        var hungarian = Samples.Read(Samples.Hungarian);

        var paths = ParityValidator.Compare(("resume.en.json", Samples.Read(Samples.English)), [("resume.hu.json", hungarian with { Work = [hungarian.Work[0]] })])
            .Select(issue => issue.Path).ToList();

        paths.ShouldContain("/work");
        paths.ShouldContain("/work/1/x-id");
    }

    [Fact]
    public void ReportsExtraItemGivenAddedSkillEntry()
    {
        var hungarian = Samples.Read(Samples.Hungarian);

        var paths = ParityValidator.Compare(("resume.en.json", Samples.Read(Samples.English)), [("resume.hu.json", hungarian with { Skills = [.. hungarian.Skills, hungarian.Skills[0]] })])
            .Select(issue => issue.Path).ToList();

        paths.ShouldContain("/skills");
        paths.ShouldContain("/skills/3/keywords");
    }
}
