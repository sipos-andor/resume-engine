using Sipos.Resume.Theme.Operandor.Rendering;
using Sipos.Resume.Theme.Operandor.Tests.Helpers;

namespace Sipos.Resume.Theme.Operandor.Tests.Rendering;

public class ResumeSeoPerson
{
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
