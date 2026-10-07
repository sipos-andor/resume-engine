using QuestPDF.Infrastructure;

namespace Sipos.Resume.Documents.Pdf;

/// <summary>How the PDF writers run.</summary>
/// <param name="License">
/// The QuestPDF licence the host renders under, such as <see cref="LicenseType.Community"/>; the writers apply it to
/// <see cref="QuestPDF.Settings.License"/>.
/// </param>
/// <remarks>
/// Decision: the host names the QuestPDF licence; the package sets none of its own.
/// Why: the engine is MIT, but QuestPDF has its own licence whose Community tier depends on the user's revenue. Only
/// the host knows which tier applies, and a default chosen here would claim it for every consumer.
/// Considered: setting <see cref="LicenseType.Community"/> in the writers, which is right for this CV and wrong for a
/// company above the threshold that installs the package.
/// </remarks>
public sealed record PdfWriterOptions(LicenseType License);
