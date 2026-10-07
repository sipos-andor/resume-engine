namespace Sipos.Resume.Core.Validation;

/// <summary>A problem found in a content file, located well enough to fix it.</summary>
/// <param name="Source">The file, such as <c>resume.hu.json</c>.</param>
/// <param name="Path">The JSON pointer of the value, such as <c>/work/2/startDate</c>; empty for the whole file.</param>
/// <param name="Message">What is wrong, in a sentence.</param>
public sealed record ValidationIssue(string Source, string Path, string Message)
{
    /// <summary>Returns the issue as one line: file, pointer and message.</summary>
    public override string ToString() => Path.Length == 0 ? $"{Source}: {Message}" : $"{Source}{Path}: {Message}";
}
