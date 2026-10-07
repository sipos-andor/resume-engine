using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Artifacts;

/// <summary>A check a share-image writer makes of the text it draws before any image is written.</summary>
public interface IShareImageContentCheck
{
    /// <summary>Returns the problems the writer would have with one language's share-image content.</summary>
    /// <param name="edition">The checked content of one language.</param>
    /// <param name="theme">The documents' look, or <see langword="null"/> when no theme is set.</param>
    /// <param name="site">The site's origin.</param>
    IEnumerable<ValidationIssue> Check(ResumeEdition edition, DocumentTheme? theme, Uri site);
}
