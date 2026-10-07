using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Tests.Helpers;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Tests.Validation;

public class ResumeValidatorValidate
{
    private static IReadOnlyList<string> PathsOf(JsonResume resume) =>
        [.. ResumeValidator.Validate("resume.en.json", resume).Select(issue => issue.Path)];

    [Fact]
    public void FindsNothingGivenValidSample() => PathsOf(Samples.Read(Samples.English)).ShouldBeEmpty();

    [Fact]
    public void RefusesEmailGivenBasicsEmail()
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Basics = resume.Basics! with { Email = "x" } }).ShouldBe(["/basics/email"]);
    }

    [Fact]
    public void NeedsNameGivenNoName()
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Basics = resume.Basics! with { Name = " " } }).ShouldBe(["/basics/name"]);
    }

    [Theory]
    [InlineData("2022-13", null, "/work/0/startDate")]
    [InlineData("2022-11", "2021", "/work/0/endDate")]
    [InlineData(null, null, "/work/0/startDate")]
    public void ChecksPeriodGivenWrongDates(string? start, string? end, string path)
    {
        var resume = Samples.Read(Samples.English);
        var work = resume.Work.ToList();
        work[0] = work[0] with { StartDate = start, EndDate = end };

        PathsOf(resume with { Work = work }).ShouldBe([path]);
    }

    [Fact]
    public void FindsUnknownPositionGivenProjectOfMissingWork()
    {
        var resume = Samples.Read(Samples.English);
        var projects = resume.Projects.ToList();
        projects[0] = projects[0] with { Work = "nowhere" };

        PathsOf(resume with { Projects = projects }).ShouldBe(["/projects/0/x-work"]);
    }

    [Fact]
    public void FindsDuplicateGivenSameIdentifierTwice()
    {
        var resume = Samples.Read(Samples.English);
        var projects = resume.Projects.ToList();
        projects[1] = projects[1] with { Id = "acme" };

        PathsOf(resume with { Projects = projects }).ShouldBe(["/projects/1/x-id"]);
    }

    [Fact]
    public void FindsUnknownFocusGivenUndefinedProfile()
    {
        var resume = Samples.Read(Samples.English);
        var work = resume.Work.ToList();
        work[1] = work[1] with { Focus = ["designer"] };

        PathsOf(resume with { Work = work }).ShouldBe(["/work/1/x-focus/0"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void RefusesRatingGivenOutOfRange(int rating)
    {
        var resume = Samples.Read(Samples.English);
        var skills = resume.Skills.ToList();
        skills[0] = skills[0] with { Rating = rating };

        PathsOf(resume with { Skills = skills }).ShouldBe(["/skills/0/x-rating"]);
    }

    [Fact]
    public void RefusesRelativeAddressGivenProfileUrl()
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Basics = resume.Basics! with { Profiles = [new JsonResumeProfile { Network = "GitHub", Url = "github.com/ann" }] } })
            .ShouldBe(["/basics/profiles/0/url"]);
    }

    [Fact]
    public void NeedsDateGivenAvailableFrom()
    {
        var resume = Samples.Read(Samples.English);
        var basics = resume.Basics! with { Availability = resume.Basics.Availability! with { Status = "from" } };

        PathsOf(resume with { Basics = basics }).ShouldBe(["/basics/x-availability/from"]);
    }

    // A plain JSON Resume has no identifiers; the engine makes them from the name and date.
    [Fact]
    public void AcceptsItemsWithoutIdentifiersGivenPlainJsonResume()
    {
        var resume = Samples.Read(Samples.English);
        var work = resume.Work.Select(item => item with { Id = null }).ToList();
        var projects = resume.Projects.Select(item => item with { Id = null, Work = null }).ToList();

        PathsOf(resume with { Work = work, Projects = projects }).ShouldBeEmpty();
    }
}
