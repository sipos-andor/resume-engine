using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Generation.Tests.Helpers;

internal sealed class RefusingShareImageWriter : IShareImageWriter, IShareImageContentCheck
{
    public IEnumerable<ValidationIssue> Check(ResumeEdition edition, DocumentTheme? theme, Uri site) =>
        [new ValidationIssue(edition.FileName, "/basics/name", "Has characters the share image cannot draw.")];

    public byte[] Write(Sipos.Resume.Core.Model.ResumeDocument document, DocumentTheme theme, Uri site) => [];
}
