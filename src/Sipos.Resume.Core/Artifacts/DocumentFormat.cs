namespace Sipos.Resume.Core.Artifacts;

/// <summary>A file format a CV is published in.</summary>
public enum DocumentFormat
{
    /// <summary>PDF.</summary>
    Pdf,

    /// <summary>Word (Office Open XML).</summary>
    Docx,

    /// <summary>Plain UTF-8 text.</summary>
    PlainText,

    /// <summary>Markdown.</summary>
    Markdown,

    /// <summary>JSON Resume.</summary>
    JsonResume,
}
