using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Tests.Validation;

public class ParityValidatorCompare
{
    // Texts are translated, facts are not: names, titles and labels may differ.
    [Fact]
    public void FindsNothingGivenTranslatedTextsOnly() =>
        ParityValidator.Compare(("resume.en.json", Samples.Read(Samples.English)), [("resume.hu.json", Samples.Read(Samples.Hungarian))]).ShouldBeEmpty();

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
