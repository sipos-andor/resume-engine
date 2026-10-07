namespace Sipos.Resume.Core.Artifacts;

/// <summary>How a document is laid out.</summary>
public enum DocumentVariant
{
    /// <summary>In the site's design: colours, the brand's fonts and marks, level meters.</summary>
    Designed,

    /// <summary>For applicant tracking systems: one column, standard headings, plain bullets, levels in words.</summary>
    Ats,
}
