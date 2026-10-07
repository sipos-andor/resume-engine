using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Sipos.Resume.Generation.Tests.Arch;

public class GenerationTypesShould
{
    private static readonly Architecture Architecture = new ArchLoader().LoadAssemblies(typeof(ResumeGenerator).Assembly).Build();

    // The build reaches the PDF writer and the theme only through the core's ports, so a consumer picks both, and
    // QuestPDF's licence never comes with the build.
    [Fact]
    public void ReachPdfAndThemeOnlyThroughPorts() =>
        Types().That().ResideInNamespaceMatching(@"^Sipos\.Resume\.Generation(\.|$)").As("build types")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(Sipos\.Resume\.Documents|Sipos\.Resume\.Theme|QuestPDF|Operandor)(\.|$)"))
            .Check(Architecture);

    // Only the DOCX writer works with OpenXml; the rest of the build stays independent of it.
    [Fact]
    public void KeepOpenXmlInDocxWriter() =>
        Types().That().ResideInNamespaceMatching(@"^Sipos\.Resume\.Generation(\.|$)").And().DoNotResideInNamespace("Sipos.Resume.Generation.Documents.Docx").As("build types outside the DOCX writer")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^DocumentFormat\.OpenXml(\.|$)"))
            .Check(Architecture);
}
