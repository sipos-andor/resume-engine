using Sipos.Resume.Core.Dates;
using Sipos.Resume.Theme.Operandor.Rendering;
using Sipos.Resume.Theme.Operandor.Tests.Helpers;

namespace Sipos.Resume.Theme.Operandor.Tests.Rendering;

public class ResumeSeoPerson
{
    [Theory]
    [InlineData("2026", true)]
    [InlineData("2026-10", true)]
    [InlineData("2026-10-07", true)]
    [InlineData("2026-10-08", false)]
    [InlineData("2026-11", false)]
    [InlineData("2027", false)]
    public void NamesOnlyEmployersWhosePositionHasStarted(string start, bool expected)
    {
        var page = ThemePages.Sample(today: new DateOnly(2026, 10, 7))[0];
        var position = page.Document.Positions[0] with { Url = "https://example.com/employer", Period = new DateRange(PartialDate.Parse(start)) };

        var person = ResumeSeo.Person(page with { Document = page.Document with { Positions = [position] } });

        if (expected)
        {
            person.WorksFor!.Id.ShouldBe("https://example.com/employer#organization");
        }
        else
        {
            person.WorksFor.ShouldBeNull();
        }
    }

    [Fact]
    public void SkipsFutureEmployerWhenSelectingCurrentEmployer()
    {
        var page = ThemePages.Sample()[0];
        var current = page.Document.Positions[0] with { Url = "https://example.com/current" };
        var future = current with { Organization = "Future", Url = "https://example.com/future", Period = new DateRange(new PartialDate(2027)) };

        var person = ResumeSeo.Person(page with { Document = page.Document with { Positions = [future, current] } });

        person.WorksFor!.Id.ShouldBe("https://example.com/current#organization");
    }

    [Fact]
    public void DescribesPersonFromCvGivenPage()
    {
        var person = ResumeSeo.Person(ThemePages.Sample()[0]);

        person.Id.ShouldBe("https://cv.example.com/#person");
        person.Telephone.ShouldBe("+36301234567");
        person.SameAs.ShouldBe(["https://github.com/ann"]);
        person.KnowsAbout.ShouldBe(["C#", "LINQ", "Azure"]);
        person.ImagePath.ShouldBe("/og/en.png");
    }

    // operandor.io's own structured data names the company by this identifier, so both sites mean one entity.
    [Fact]
    public void NamesOperandorByItsIdentifierGivenOperandorPosition()
    {
        var page = ThemePages.Sample()[0];
        var document = page.Document with { Positions = [page.Document.Positions[0] with { Organization = "operandor", Url = "https://operandor.io/" }] };

        ResumeSeo.Person(page with { Document = document }).WorksFor!.Id.ShouldBe("https://operandor.io/#organization");
    }

    [Theory]
    [InlineData("https://example.com/company?ref=cv", "https://example.com/company?ref=cv#organization")]
    [InlineData("https://example.com/company#old", "https://example.com/company#organization")]
    public void UsesOrganizationUriFragmentGivenCompanyUrl(string url, string expected)
    {
        var page = ThemePages.Sample()[0];
        var document = page.Document with { Positions = [page.Document.Positions[0] with { Url = url }] };

        ResumeSeo.Person(page with { Document = document }).WorksFor!.Id.ShouldBe(expected);
    }

    [Fact]
    public void CutsDescriptionAtWordGivenLongFirstSentence()
    {
        var page = ThemePages.Sample()[0];
        var summary = string.Join(' ', Enumerable.Repeat("modernisation", 20)) + ". Second.";
        var document = page.Document with { Person = page.Document.Person with { Summary = summary } };

        var description = ResumeSeo.Description(page with { Document = document })!;

        description.Length.ShouldBeLessThanOrEqualTo(160);
        description.ShouldEndWith("modernisation…");
    }

    // Croatian, Serbian and Hungarian write years and ordinals with a dot; the first sentence ends before a capital.
    [Theory]
    [InlineData("Od 2013. godine razvijam .NET sustave. Drugo.", "Od 2013. godine razvijam .NET sustave.")]
    [InlineData("Az ALLWIN Kft. vezető fejlesztője 2015. március óta. Második.", "Az ALLWIN Kft. vezető fejlesztője 2015. március óta.")]
    [InlineData("Builds .NET systems. As founder, leads.", "Builds .NET systems.")]
    public void EndsDescriptionAtFirstSentenceGivenOrdinalsAndAbbreviations(string summary, string description)
    {
        var page = ThemePages.Sample()[0];
        var document = page.Document with { Person = page.Document.Person with { Summary = summary } };

        ResumeSeo.Description(page with { Document = document }).ShouldBe(description);
    }

    // A summer school is no degree; the structured data must not claim one.
    [Fact]
    public void CallsOnlyDegreesDegreesGivenEducation()
    {
        var page = ThemePages.Sample()[0];
        var study = page.Document.Education[0];
        var document = page.Document with { Education = [study, study with { StudyType = "Summer school", Area = "Web development" }, study with { StudyType = "Prvostupnik (BSc)" }] };

        ResumeSeo.Person(page with { Document = document }).Credentials.Take(3).Select(credential => credential.Category).ShouldBe(["degree", null, "degree"]);
    }
}
