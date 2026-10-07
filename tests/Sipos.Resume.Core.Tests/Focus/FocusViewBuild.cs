using Sipos.Resume.Core.Focus;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Tests.Helpers;

namespace Sipos.Resume.Core.Tests.Focus;

public class FocusViewBuild
{
    [Fact]
    public void PutsForwardTaggedItemsAndItemsUsingProfileSkills()
    {
        var view = FocusView.Build(Documents.English()).Single();

        view.Profile.Id.ShouldBe("architect");
        view.Emphasized.Order().ShouldBe(["acme", "portal"]);
        view.EmphasizedSkills.ShouldBe(["azure"]);
    }

    [Fact]
    public void OrdersGroupsWithProfileSkillsFirst() => FocusView.Build(Documents.English()).Single().SkillGroupOrder.ShouldBe([1, 0]);

    [Fact]
    public void OrdersTaggedStrengthsFirstAndKeepsOrderOtherwise()
    {
        var document = Documents.English();
        Strength[] strengths = [new("a", "A", null, false, []), new("b", "B", null, false, ["architect"]), new("c", "C", null, false, [])];

        FocusView.Build(document with { Strengths = strengths }).Single().StrengthOrder.ShouldBe(["b", "a", "c"]);
    }
}
