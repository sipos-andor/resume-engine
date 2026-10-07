using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Generation.Documents.Docx;

namespace Sipos.Resume.Generation.Tests.Documents.Docx;

public class DocxDocumentWriterSupports
{
    [Theory]
    [InlineData(DocumentVariant.Designed)]
    [InlineData(DocumentVariant.Ats)]
    public void SupportsLayoutGivenDocxFormat(DocumentVariant variant)
    {
        var writer = new DocxDocumentWriter();

        writer.Format.ShouldBe(DownloadFormat.Docx);
        writer.Supports(variant).ShouldBeTrue();
    }
}
