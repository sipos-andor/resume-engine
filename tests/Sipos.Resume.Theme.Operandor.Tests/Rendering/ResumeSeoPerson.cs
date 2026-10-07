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
}
