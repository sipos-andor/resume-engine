using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Sipos.Resume.Theme.Operandor.Tests.Arch;

public class ThemeTypesShould
{
    private static readonly Architecture Architecture = new ArchLoader().LoadAssemblies(typeof(OperandorTheme).Assembly).Build();

    // The theme renders in the build and in the browser's Studio, so it knows neither the build nor a document library.
    [Fact]
    public void NotDependOnBuildOrDocumentLibraries() =>
        Types().That().ResideInNamespaceMatching(@"^Sipos\.Resume\.Theme\.Operandor(\.|$)").As("theme types")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(Sipos\.Resume\.Generation|Sipos\.Resume\.Documents|QuestPDF|DocumentFormat)(\.|$)"))
            .Check(Architecture);
}
