using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Mapping;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Tests.Mapping;

public class ResumeMapperMap
{
    private static readonly ResumeLanguage English = LanguageCatalog.Describe("en", null, isDefault: true);

    private static ResumeDocument Document() => ResumeMapper.Map(Samples.Read(Samples.English), English);

    [Fact]
    public void NestsProjectsUnderTheirPosition()
    {
        var document = Document();

        document.Positions[0].Engagements.Select(e => e.Id).ShouldBe(["portal"]);
        document.Positions[1].Engagements.ShouldBeEmpty();
        document.Projects.Select(e => e.Id).ShouldBe(["hobby"]);
        document.AllEngagements.Select(e => e.Id).ShouldBe(["portal", "hobby"]);
    }

    [Fact]
    public void ReadsPeriodsGivenDates()
    {
        var document = Document();

        document.Positions[0].Period.ShouldBe(new DateRange(PartialDate.Parse("2022-11")));
        document.Positions[0].Period.IsOngoing.ShouldBeTrue();
        document.Positions[1].Period.End.ShouldBe(PartialDate.Parse("2022-10"));
        document.Projects[0].Period.ShouldBeNull();
    }

    // One group per name, each skill with its entry's level, in the file's order.
    [Fact]
    public void MergesSkillEntriesByGroupName()
    {
        var groups = Document().SkillGroups;

        groups.Select(g => g.Name).ShouldBe([".NET platform", "Cloud"]);
        groups[0].Skills.Select(s => (s.Name, s.Rating, s.Level)).ShouldBe([("C#", 5, "Expert"), ("LINQ", 5, "Expert"), ("Blazor", 3, "Proficient")]);
    }

    [Fact]
    public void MapsPersonWithContactAvailabilityAndPhone()
    {
        var person = Document().Person;

        person.Location.ShouldBe("Remote from Europe");
        person.Contact.ShouldBe(new Link("https://example.com/contact", "Get in touch"));
        person.Availability!.Status.ShouldBe(AvailabilityStatus.Available);
        person.PhoneDial.ShouldBe("+36301234567");
        person.Profiles.Single().Network.ShouldBe("GitHub");
    }

    // A + only when the CV writes one: a local number with a + in front would dial another, invalid number.
    [Theory]
    [InlineData(" +44 20 7946 0000", "+442079460000")]
    [InlineData("020 7946 0000", "02079460000")]
    [InlineData("(06) 30/123-4567", "06301234567")]
    [InlineData("on request", null)]
    [InlineData("+1 555 123 4567 ext. 89", null)]
    [InlineData("+1 555 123 4567 x89", null)]
    [InlineData("+1 555 123 4567;ext=89", null)]
    [InlineData("555+123", null)]
    [InlineData("++555123", null)]
    [InlineData("+1 (555) 123.4567", "+15551234567")]
    public void DialsNumberAsWrittenGivenPhone(string phone, string? dial)
    {
        var resume = Samples.Read(Samples.English);

        ResumeMapper.Map(resume with { Basics = resume.Basics! with { Phone = phone } }, English).Person.PhoneDial.ShouldBe(dial);
    }

    // JSON Resume files usually give a graduation date alone.
    [Fact]
    public void KeepsGraduationYearGivenStudyWithOnlyEndDate()
    {
        var period = Document().Education.Single().Period.ShouldNotBeNull();

        (period.Start, period.End).ShouldBe((PartialDate.Parse("2012"), PartialDate.Parse("2012")));
    }

    [Fact]
    public void ReadsLastModifiedGivenMeta() => Document().LastModified.ShouldBe(new DateOnly(2026, 10, 7));

    [Fact]
    public void MakesIdentifiersGivenNoXId()
    {
        var resume = Samples.Read(Samples.English);
        var plain = resume with { Work = [.. resume.Work.Select(w => w with { Id = null })], Projects = [] };

        ResumeMapper.Map(plain, English).Positions.Select(p => p.Id).ShouldBe(["acme-2022-11", "initech-2015-03"]);
    }

    [Fact]
    public void MapsPublicationsVolunteeringAndStudyDetailsGivenSample()
    {
        var document = Document();

        var publication = document.Publications.ShouldHaveSingleItem();
        (publication.Name, publication.Publisher, publication.Summary).ShouldBe(("Practical .NET", "Example Press", "Wrote two chapters."));
        publication.Released.ShouldNotBeNull().Year.ShouldBe(2019);
        var work = document.Volunteering.ShouldHaveSingleItem();
        (work.Title, work.Organization, work.Summary).ShouldBe(("Volunteer", "Food Bank", "Sorted donations."));
        var period = work.Period.ShouldNotBeNull();
        period.End.ShouldBe(period.Start);
        document.Education[0].Score.ShouldBe("4.5/5");
        document.Education[0].Courses.ShouldBe(["Databases", "Networks"]);
    }

    // Without a position the organization names the work.
    [Fact]
    public void NamesVolunteerWorkByOrganizationGivenNoPosition()
    {
        var resume = Samples.Read(Samples.English);

        ResumeMapper.Map(resume with { Volunteer = [resume.Volunteer[0] with { Position = null }] }, English).Volunteering[0].Title.ShouldBe("Food Bank");
    }

    // The legend reads from the highest rating down, in whatever order the file lists the steps.
    [Fact]
    public void MapsScaleFromHighestRatingAndUsageKindGivenAssessment()
    {
        var usage = ResumeMapper.Map(Samples.Read(Samples.Usage), English);

        usage.SkillScale!.Steps.Select(step => (step.Rating, step.Label)).ShouldBe([(5, "Usage: core and recurring"), (3, "Usage: project-specific")]);
        usage.SkillScale.Note.ShouldBe("Scores describe usage, not proficiency.");
        usage.AllSkills.ShouldAllBe(skill => skill.Kind == RatingKind.Usage);
        Document().SkillScale.ShouldBeNull();
        Document().AllSkills.ShouldAllBe(skill => skill.Kind == RatingKind.Proficiency);
    }

    // A confirmed use names its position or project, and its own period when the file gives one.
    [Fact]
    public void MapsConfirmedAssociationsWithPeriodGivenDates()
    {
        var json = Samples.Usage.Replace(
            """{ "skill": "Blazor", "work": "acme" }""",
            """{ "skill": "Blazor", "work": "acme", "startDate": "2024", "endDate": "2025-06" }, { "skill": "Azure", "project": "portal" }""",
            StringComparison.Ordinal);

        var associations = ResumeMapper.Map(Samples.Read(json), English).SkillAssociations;

        associations.ShouldBe(
        [
            new SkillAssociation("Blazor", "acme", new DateRange(new PartialDate(2024), new PartialDate(2025, 6))),
            new SkillAssociation("Azure", "portal"),
        ]);
    }
}
