using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Generation.Documents.Docx;

namespace Sipos.Resume.Generation.Tests.Documents.Docx;

/// <summary>Writes the sample CVs as DOCX files and reads them back.</summary>
internal static class DocxTestFiles
{
    public static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public static readonly Uri PageUrl = new("https://cv.example.com/hu/");

    public static readonly DocumentTheme Theme = new("14181C", "333833", "636A61", "7F857C", "2D7612", "D6D9D3", "IBM Plex Sans", "IBM Plex Mono");

    public static ResumeDocument Sample(string language) => language switch
    {
        "hu" => SampleDocuments.Hungarian(),
        "sr" => Serbian(),
        _ => SampleDocuments.English(),
    };

    // Serbian Latin with the letters a font or an encoding drops first: č ć đ š ž.
    public static ResumeDocument Serbian()
    {
        var english = SampleDocuments.English();
        return english with
        {
            Language = LanguageCatalog.Describe("sr-Latn", null, isDefault: false),
            Person = english.Person with { Name = "Đorđe Šćepanović", Title = "Softverski arhitekta", Summary = "Gradi .NET sisteme: čak i žičane mreže." },
        };
    }

    public static DocumentContext Context(ResumeDocument document, DocumentVariant variant, string? focusId = null, string? email = null)
    {
        var insights = ResumeInsights.Analyze(document, SampleDocuments.Today);
        var download = DownloadCatalog.For(document.Language, "Ann_Example_CV", document.FocusProfiles.Select(profile => profile.Id))
            .Single(spec => spec.Format == DownloadFormat.Docx && spec.Variant == variant && spec.FocusId == focusId);
        var focus = focusId is null ? null : insights.Focus.Single(view => view.Profile.Id == focusId);
        return new DocumentContext(document, insights, download, focus, PageUrl, Theme, email);
    }

    public static byte[] Write(DocumentContext context)
    {
        using var stream = new MemoryStream();
        new DocxDocumentWriter().Write(context, stream);
        return stream.ToArray();
    }

    public static byte[] Write(string language, DocumentVariant variant, string? focusId = null) => Write(Context(Sample(language), variant, focusId));

    public static WordprocessingDocument Open(byte[] bytes) => WordprocessingDocument.Open(new MemoryStream(bytes), isEditable: false);

    /// <summary>Every entry of the package as text, by name.</summary>
    public static Dictionary<string, string> Parts(byte[] bytes)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes));
        return zip.Entries.ToDictionary(entry => entry.FullName, entry =>
        {
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            return reader.ReadToEnd();
        });
    }

    public static XDocument Part(byte[] bytes, string name) => XDocument.Parse(Parts(bytes)[name]);

    /// <summary>The body's paragraphs with their style and text; a tab adds no text.</summary>
    public static List<(string Style, string Text)> Paragraphs(byte[] bytes)
    {
        using var document = Open(bytes);
        return [.. document.MainDocumentPart!.Document!.Body!.Descendants<Paragraph>()
            .Select(paragraph => (paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "Normal", paragraph.InnerText))];
    }

    public static List<string> Texts(byte[] bytes, string style) =>
        [.. Paragraphs(bytes).Where(paragraph => paragraph.Style == style).Select(paragraph => paragraph.Text)];
}
