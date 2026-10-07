using System.Text;
using Sipos.Resume.Core.Artifacts;

namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>A writer of one format that records what it was given and writes a placeholder, or the e-mail address if told to leak it.</summary>
internal sealed class RecordingWriter(DownloadFormat format, bool leakEmail = false) : IDocumentWriter
{
    public List<DocumentContext> Contexts { get; } = [];

    public DownloadFormat Format => format;

    public bool Supports(DocumentVariant variant) => true;

    public void Write(DocumentContext context, Stream output)
    {
        Contexts.Add(context);
        output.Write(Encoding.UTF8.GetBytes(leakEmail ? $"contact {context.ContactEmail}" : $"{format} of {context.Download.FileName}"));
    }
}
