using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Sipos.Resume.Core.Model;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Sipos.Resume.Core.Tests.Arch;

public class CoreTypesShould
{
    private static readonly Architecture Architecture = new ArchLoader().LoadAssemblies(typeof(ResumeDocument).Assembly).Build();

    // The core runs at build time and in the browser, so it uses neither a web framework nor a document library.
    [Fact]
    public void NotDependOnWebOrDocumentFrameworks() =>
        Types().That().ResideInNamespaceMatching(@"^Sipos\.Resume\.Core(\.|$)").As("core types")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^(Microsoft\.AspNetCore|Microsoft\.JSInterop|QuestPDF|DocumentFormat|Operandor)(\.|$)"))
            .Check(Architecture);

    // The domain model is plain data, independent of how a file is read.
    [Fact]
    public void KeepModelFreeOfContentFormat() =>
        Types().That().ResideInNamespace("Sipos.Resume.Core.Model").As("model types")
            .Should().NotDependOnAny(Types().That().ResideInNamespace("Sipos.Resume.Core.Content"))
            .Check(Architecture);
}
