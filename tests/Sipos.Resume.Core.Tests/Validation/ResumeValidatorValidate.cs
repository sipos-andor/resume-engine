using Sipos.Resume.Core.Content;
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
    [InlineData("call me")]
    [InlineData("+ () -")]
    [InlineData("１２３")]
    [InlineData("١٢٣")]
    public void RefusesPhoneGivenNoAsciiDigit(string phone)
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Basics = resume.Basics! with { Phone = phone } }).ShouldBe(["/basics/phone"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("+36 (30) 903-6622")]
    [InlineData("020 7946 0000")]
    [InlineData("0")]
    public void AcceptsPhoneGivenBlankOrAsciiDigits(string? phone)
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Basics = resume.Basics! with { Phone = phone } }).ShouldBeEmpty();
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
    public void ChecksEducationPeriodGivenEndBeforeStart()
    {
        var resume = Samples.Read(Samples.English);
        var education = resume.Education.ToList();
        education[0] = education[0] with { StartDate = "2015", EndDate = "2012" };

        PathsOf(resume with { Education = education }).ShouldBe(["/education/0/endDate"]);
    }

    [Fact]
    public void AllowsEmptyEducationDatesGivenOptionalFields()
    {
        var resume = Samples.Read(Samples.English);
        var education = resume.Education.ToList();
        education[0] = education[0] with { StartDate = "", EndDate = "" };

        PathsOf(resume with { Education = education }).ShouldBeEmpty();
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
    public void RefusesProjectEndDateGivenNoStartDate()
    {
        var resume = Samples.Read(Samples.English);
        var projects = resume.Projects.ToList();
        projects[0] = projects[0] with { StartDate = null, EndDate = "2025" };

        PathsOf(resume with { Projects = projects }).ShouldBe(["/projects/0/startDate"]);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "")]
    [InlineData("2024", null)]
    [InlineData("2024", "2025")]
    public void AcceptsProjectGivenNoDatesOrStartDate(string? start, string? end)
    {
        var resume = Samples.Read(Samples.English);
        var projects = resume.Projects.ToList();
        projects[0] = projects[0] with { StartDate = start, EndDate = end };

        PathsOf(resume with { Projects = projects }).ShouldBeEmpty();
    }

    [Fact]
    public void AllowsEducationEndDateGivenNoStartDate()
    {
        var resume = Samples.Read(Samples.English);
        var education = resume.Education.ToList();
        education[0] = education[0] with { StartDate = null, EndDate = "2025" };

        PathsOf(resume with { Education = education }).ShouldBeEmpty();
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

    // A section, a generated anchor or a theme's own id would give the page two elements with one id.
    [Theory]
    [InlineData("skills")]
    [InlineData("education-2")]
    [InlineData("cv-search")]
    [InlineData("portal-title")]
    public void RefusesIdentifierGivenOneThePageUsesForItself(string id)
    {
        var resume = Samples.Read(Samples.English);
        var projects = resume.Projects.ToList();
        projects[1] = projects[1] with { Id = id };

        PathsOf(resume with { Projects = projects }).ShouldBe(["/projects/1/x-id"]);
    }

    // Positions, projects and strengths are anchors of one page.
    [Fact]
    public void FindsDuplicateGivenStrengthWithIdentifierOfPosition()
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Strengths = [resume.Strengths[0] with { Id = "initech" }] }).ShouldBe(["/x-strengths/0/id"]);
    }

    // "ats" would name its downloads _Ats next to the ATS files, and "archi-tect" like "architect" on a disk that ignores case.
    [Theory]
    [InlineData("ats")]
    [InlineData("archi-tect")]
    public void RefusesProfileGivenIdentifierThatNamesItsDownloadsLikeAnotherFile(string id)
    {
        var resume = Samples.Read(Samples.English);
        JsonResumeFocusProfile other = new() { Id = id, Label = "Other" };

        PathsOf(resume with { FocusProfiles = [.. resume.FocusProfiles, other] }).ShouldBe(["/x-focusProfiles/1/id"]);
    }

    // Two spellings of one term would be one technology with two alias lists; the pointer escapes the term's slash.
    [Fact]
    public void RefusesTermGivenSpellingOfAnotherTerm()
    {
        var resume = Samples.Read(Samples.English);
        var aliases = new Dictionary<string, IReadOnlyList<string>> { ["CI CD"] = ["pipelines"], ["CI/CD"] = ["devops"] };

        PathsOf(resume with { Aliases = aliases }).ShouldBe(["/x-aliases/CI~1CD"]);
    }

    // The filter gives an alias to its first term, the job ad matcher to every term that lists it; they must agree.
    [Theory]
    [InlineData("csharp", "/x-aliases/F#/0")]
    [InlineData("c#", "/x-aliases/F#/0")]
    public void RefusesAliasGivenAnotherTermOrItsAlias(string alias, string path)
    {
        var resume = Samples.Read(Samples.English);
        var aliases = new Dictionary<string, IReadOnlyList<string>> { ["C#"] = ["csharp"], ["F#"] = [alias] };

        PathsOf(resume with { Aliases = aliases }).ShouldBe([path]);
    }

    [Fact]
    public void RefusesTermGivenNoLettersOrDigits()
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Aliases = new Dictionary<string, IReadOnlyList<string>> { ["!?"] = ["what"] } }).ShouldBe(["/x-aliases/!?"]);
    }

    // The mapper reads the first ten characters as YYYY-MM-DD; any other form would pass here and fail there.
    [Theory]
    [InlineData("2026.10.07")]
    [InlineData("10/07/2026")]
    [InlineData("2026-1-7")]
    public void RefusesLastModifiedGivenOtherThanIsoDate(string date)
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Meta = resume.Meta! with { LastModified = date } }).ShouldBe(["/meta/lastModified"]);
    }

    [Theory]
    [InlineData("SR")]
    [InlineData("sr/latn")]
    [InlineData("-sr")]
    public void RefusesPathGivenOtherThanOneLowercaseSegment(string path)
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Meta = resume.Meta! with { Path = path } }).ShouldBe(["/meta/x-path"]);
    }

    // JSON may escape any control character, but a Word document cannot hold one; tab and line breaks are fine.
    [Theory]
    [InlineData("Builds\u0001 systems.", true)]
    [InlineData("Builds\u0000 systems.", true)]
    [InlineData("Builds\tsystems.\r\nWell.", false)]
    public void RefusesControlCharacterGivenTextDocumentsCannotHold(string summary, bool refused)
    {
        var resume = Samples.Read(Samples.English);

        PathsOf(resume with { Basics = resume.Basics! with { Summary = summary } }).ShouldBe(refused ? ["/basics/summary"] : []);
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
