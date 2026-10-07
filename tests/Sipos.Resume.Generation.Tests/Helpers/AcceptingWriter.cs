using Sipos.Resume.Core.Artifacts;

namespace Sipos.Resume.Generation.Tests.Helpers;

internal sealed class AcceptingWriter : IDocumentWriter
{
    public DownloadFormat Format => DownloadFormat.Pdf;

    public bool Supports(DocumentVariant variant) => true;

    public void Write(DocumentContext context, Stream output) { }
}
