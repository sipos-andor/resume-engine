using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Evidence;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Tests.Evidence;

public class SkillEvidenceBuild
{
    [Fact]
    public void ListsItemsAndYearsGivenUsedSkill()
    {
        var azure = SkillEvidence.Build(SampleDocuments.English())["Azure"];

        azure.Items.Select(item => (item.Id, item.Title)).ShouldBe([("portal", "Portal – Globex")]);
        azure.FirstYear.ShouldBe(2023);
        azure.LastYear.ShouldBe(2023);
        azure.Ongoing.ShouldBeFalse();
    }

    [Fact]
    public void FindsNothingGivenSkillNoItemNames()
    {
        var blazor = SkillEvidence.Build(SampleDocuments.English())["Blazor"];

        blazor.Items.ShouldBeEmpty();
        blazor.FirstYear.ShouldBeNull();
    }

    // A confirmed use is evidence its item's keywords do not show: the usage sample confirms Blazor at acme.
    [Fact]
    public void ListsPositionGivenConfirmedAssociation()
    {
        var blazor = SkillEvidence.Build(SampleDocuments.Usage())["Blazor"];

        blazor.Items.Select(item => item.Id).ShouldBe(["acme"]);
        blazor.Ongoing.ShouldBeTrue();
    }

    // A dated confirmed use says when the skill was used there, not the item's whole period: Blazor at the ongoing
    // acme only in 2024–2025.
    [Fact]
    public void TakesAssociationPeriodGivenDatedAssociation()
    {
        var document = SampleDocuments.Usage() with { SkillAssociations = [new SkillAssociation("Blazor", "acme", new DateRange(new PartialDate(2024), new PartialDate(2025)))] };

        var blazor = SkillEvidence.Build(document)["Blazor"];

        blazor.Ongoing.ShouldBeFalse();
        blazor.LastYear.ShouldBe(2025);
        blazor.FirstYear.ShouldBe(2024);
    }

    // The dated use wins over the keyword of the same item: LINQ is listed at the ongoing acme but last used in 2024.
    [Fact]
    public void TakesAssociationPeriodGivenKeywordOfSameItem()
    {
        var document = SampleDocuments.English();
        var acme = document.Positions[0] with { Keywords = ["LINQ"] };
        document = document with
        {
            Positions = [acme, document.Positions[1]],
            SkillAssociations = [new SkillAssociation("LINQ", "acme", new DateRange(new PartialDate(2024), new PartialDate(2024)))],
        };

        var linq = SkillEvidence.Build(document)["LINQ"];

        linq.Items.Select(item => item.Id).ShouldBe(["acme"]);
        linq.Ongoing.ShouldBeFalse();
        linq.LastYear.ShouldBe(2024);
    }

    [Fact]
    public void MarksOngoingGivenCurrentPositionKeyword()
    {
        var document = SampleDocuments.English();
        var acme = document.Positions[0] with { Keywords = ["LINQ"] };

        var linq = SkillEvidence.Build(document with { Positions = [acme, document.Positions[1]] })["LINQ"];

        linq.Items.Single().Id.ShouldBe("acme");
        linq.Ongoing.ShouldBeTrue();
        linq.LastYear.ShouldBeNull();
        linq.FirstYear.ShouldBe(2022);
    }
}
