using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>A PDF writer whose own check refuses every content, as one whose fonts lack a character would.</summary>
internal sealed class RefusingWriter : IDocumentWriter, IContentCheck
{
    public DownloadFormat Format => DownloadFormat.Pdf;

    public bool Supports(DocumentVariant variant) => true;

    public IEnumerable<ValidationIssue> Check(ResumeEdition edition, DocumentTheme? theme, string? contactEmail) =>
        [new ValidationIssue(edition.FileName, contactEmail is null ? "/basics/name" : "/contactEmail", "Has characters the PDF's fonts cannot draw.")];

    public void Write(DocumentContext context, Stream output) => throw new InvalidOperationException("The check should have stopped the build.");
}
