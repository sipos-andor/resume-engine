using Sipos.Resume.Core.Languages;

namespace Sipos.Resume.Core.Artifacts;

/// <summary>One downloadable file of a CV.</summary>
/// <param name="Format">The file format.</param>
/// <param name="Variant">The layout.</param>
/// <param name="FocusId">The position profile the document is tailored to, or <see langword="null"/> for the full CV.</param>
/// <param name="FileName">The file's ASCII name, such as <c>Andor_Sipos_CV_HU_ATS.pdf</c>.</param>
public sealed record DownloadSpec(DownloadFormat Format, DocumentVariant Variant, string? FocusId, string FileName)
{
    /// <summary>The site path the file is published at, such as <c>/downloads/Andor_Sipos_CV_HU.pdf</c>.</summary>
    public string Path => $"/downloads/{FileName}";

    /// <summary>The format's short name for menus and lists: <c>PDF</c>, <c>DOCX</c>, <c>TXT</c>, <c>Markdown</c> or <c>JSON Resume</c>.</summary>
    public string FormatName => Format switch
    {
        DownloadFormat.Pdf => "PDF",
        DownloadFormat.Docx => "DOCX",
        DownloadFormat.PlainText => "TXT",
        DownloadFormat.Markdown => "Markdown",
        _ => "JSON Resume",
    };

    /// <summary>Names the file for a menu or a list, such as <c>PDF</c>, <c>DOCX · ATS</c> or <c>PDF · Software architect</c>.</summary>
    /// <param name="focusLabel">The label of the file's position profile in the CV's language, for a tailored file.</param>
    public string Describe(string? focusLabel = null) =>
        FocusId is not null && focusLabel is not null ? $"{FormatName} · {focusLabel}"
        : Variant == DocumentVariant.Ats ? $"{FormatName} · ATS"
        : FormatName;

    /// <summary>The MIME type of the file.</summary>
    public string MediaType => Format switch
    {
        DownloadFormat.Pdf => "application/pdf",
        DownloadFormat.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        DownloadFormat.PlainText => "text/plain; charset=utf-8",
        DownloadFormat.Markdown => "text/markdown; charset=utf-8",
        _ => "application/json",
    };
}

/// <summary>Names the downloads of a CV in one language.</summary>
/// <remarks>
/// Decision: ASCII file names of the form <c>{prefix}_{LANG}[_ATS][_{Profile}].{ext}</c>.
/// Why: application portals and mail clients mangle spaces and accents, and a recruiter who saves several versions can
/// tell them apart by name.
/// </remarks>
public static class DownloadCatalog
{
    /// <summary>
    /// Lists every download of a language: designed PDF and DOCX, ATS PDF, DOCX and text, Markdown and JSON Resume, and
    /// a designed PDF and DOCX for every position profile.
    /// </summary>
    /// <param name="language">The CV's language.</param>
    /// <param name="prefix">The file name prefix, such as <c>Andor_Sipos_CV</c>.</param>
    /// <param name="focusIds">The position profiles' identifiers.</param>
    public static IReadOnlyList<DownloadSpec> For(ResumeLanguage language, string prefix, IEnumerable<string> focusIds)
    {
        var stem = $"{prefix}_{language.FileCode}";
        var list = new List<DownloadSpec>
        {
            new(DownloadFormat.Pdf, DocumentVariant.Designed, null, $"{stem}.pdf"),
            new(DownloadFormat.Docx, DocumentVariant.Designed, null, $"{stem}.docx"),
            new(DownloadFormat.Pdf, DocumentVariant.Ats, null, $"{stem}_ATS.pdf"),
            new(DownloadFormat.Docx, DocumentVariant.Ats, null, $"{stem}_ATS.docx"),
            new(DownloadFormat.PlainText, DocumentVariant.Ats, null, $"{stem}_ATS.txt"),
            new(DownloadFormat.Markdown, DocumentVariant.Designed, null, $"{stem}.md"),
            new(DownloadFormat.JsonResume, DocumentVariant.Designed, null, $"{stem}.json"),
        };
        foreach (var focus in focusIds)
        {
            var suffix = string.Concat(focus.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
            list.Add(new DownloadSpec(DownloadFormat.Pdf, DocumentVariant.Designed, focus, $"{stem}_{suffix}.pdf"));
            list.Add(new DownloadSpec(DownloadFormat.Docx, DocumentVariant.Designed, focus, $"{stem}_{suffix}.docx"));
        }

        return list;
    }
}
