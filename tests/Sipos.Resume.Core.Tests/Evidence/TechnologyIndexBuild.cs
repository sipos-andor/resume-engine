using Sipos.Resume.Core.Evidence;
using Sipos.Resume.Core.Tests.Helpers;

namespace Sipos.Resume.Core.Tests.Evidence;

public class TechnologyIndexBuild
{
    [Fact]
    public void ListsEveryTechnologyOfPositionsAndProjects() =>
        TechnologyIndex.Build(Documents.English()).Technologies.Select(t => (t.Name, t.ItemIds.Count)).ShouldBe([("Azure", 1), ("C#", 1), ("Rust", 1)]);

    // An alias counts as the name it stands for.
    [Fact]
    public void MergesAliasIntoItsTermGivenAlias()
    {
        var document = Documents.English();
        var hobby = document.Projects[0] with { Keywords = ["csharp"] };

        var technologies = TechnologyIndex.Build(document with { Projects = [hobby] }).Technologies;

        technologies.Single(t => t.Name == "C#").ItemIds.ShouldBe(["portal", "hobby"]);
        technologies.ShouldNotContain(t => t.Key == "csharp");
    }
}
