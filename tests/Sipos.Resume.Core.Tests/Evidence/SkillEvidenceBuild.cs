using Sipos.Resume.Core.Evidence;

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
