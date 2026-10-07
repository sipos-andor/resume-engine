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
    public void DialsNumberAsWrittenGivenPhone(string phone, string? dial)
    {
        var resume = Samples.Read(Samples.English);

        ResumeMapper.Map(resume with { Basics = resume.Basics! with { Phone = phone } }, English).Person.PhoneDial.ShouldBe(dial);
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
}
