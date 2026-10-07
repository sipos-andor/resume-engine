using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Artifacts;

/// <summary>
/// A check a writer makes of the content before anything is written, such as whether its fonts can draw every
/// character; the build runs it with the validation, so <c>--validate-only</c> finds the problem too.
/// </summary>
public interface IContentCheck
{
    /// <summary>Returns the problems the writer would have with one language's content, by JSON pointer.</summary>
    /// <param name="edition">The checked content of one language.</param>
    /// <param name="theme">The documents' look, or <see langword="null"/> when no theme is set, as in a validation-only run.</param>
    IEnumerable<ValidationIssue> Check(ResumeEdition edition, DocumentTheme? theme);
}
