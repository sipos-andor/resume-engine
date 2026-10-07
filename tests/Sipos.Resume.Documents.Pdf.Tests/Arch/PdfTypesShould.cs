using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Sipos.Resume.Documents.Pdf.Tests.Arch;

public class PdfTypesShould
{
    private static readonly Architecture Architecture = new ArchLoader().LoadAssemblies(typeof(PdfDocumentWriter).Assembly).Build();

    // The package carries QuestPDF's licence to whoever renders PDFs, and nothing of Word or the web.
    [Fact]
    public void NotDependOnOpenXmlOrAspNetCore() =>
        Types().That().ResideInNamespaceMatching(@"^Sipos\.Resume\.Documents\.Pdf(\.|$)").As("PDF types")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(DocumentFormat|Microsoft\.AspNetCore|Microsoft\.JSInterop)(\.|$)"))
            .Check(Architecture);

    [Fact]
    public void ReferenceNeitherOpenXmlNorAspNetCoreAssemblies() =>
        typeof(PdfDocumentWriter).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name)
            .ShouldNotContain(name => name!.StartsWith("DocumentFormat.OpenXml", StringComparison.Ordinal) || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
}
