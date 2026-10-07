using System.IO.Compression;
using System.Text;
using QuestPDF.Infrastructure;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Model;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Tokens;

namespace Sipos.Resume.Documents.Pdf.Tests;

/// <summary>Writes the sample CVs as PDFs and reads them back.</summary>
internal static class PdfSamples
{
    public const string Address = "privacy.probe@example.com";

    public static readonly Uri PageUrl = new("https://cv.example.com/");

    public static readonly PdfDocumentWriter Writer = new(new PdfWriterOptions(LicenseType.Community));

    public static DocumentContext Context(ResumeDocument document, DocumentVariant variant, string? email = null, string? focusId = null)
    {
        var insights = ResumeInsights.Analyze(document, SampleDocuments.Today);
        var download = DownloadCatalog.For(document.Language, "Ann_Example_CV", document.FocusProfiles.Select(profile => profile.Id))
            .First(spec => spec.Format == DownloadFormat.Pdf && spec.Variant == variant && spec.FocusId == focusId);
        var focus = focusId is null ? null : insights.Focus.Single(view => view.Profile.Id == focusId);
        return new DocumentContext(document, insights, download, focus, PageUrl, DocumentTheme.Neutral, email);
    }

    public static byte[] Write(DocumentContext context)
    {
        using var output = new MemoryStream();
        Writer.Write(context, output);
        return output.ToArray();
    }

    public static byte[] Write(DocumentVariant variant, string? email = null) => Write(Context(SampleDocuments.English(), variant, email));

    // Words joined in content order, with the spaces the layout implies, as a parser reads them.
    public static string Text(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
    }

    public static int ImageCount(byte[] pdf, int pageNumber = 1)
    {
        using var document = PdfDocument.Open(pdf);
        return document.GetPage(pageNumber).GetImages().Count();
    }

    public static IReadOnlyList<string> LinkTargets(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return [.. document.GetPages().SelectMany(page => page.GetAnnotations())
            .Select(annotation => annotation.Action is UglyToad.PdfPig.Actions.UriAction uri ? uri.Uri : null)
            .OfType<string>()];
    }

    public static string? CatalogString(byte[] pdf, string key)
    {
        using var document = PdfDocument.Open(pdf);
        return document.Structure.Catalog.CatalogDictionary.TryGet(NameToken.Create(key), out var token) && Resolve(document, token) is StringToken text
            ? text.Data
            : null;
    }

    // The base font name and whether a font file is embedded, for every font a page draws text with.
    public static IReadOnlyList<(string Name, bool Embedded)> Fonts(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        var references = document.GetPages().SelectMany(page => page.Letters).Select(letter => letter.FontDetails.FontDictionaryReference).OfType<IndirectReference>().Distinct().ToList();
        return [.. references.Select(reference =>
        {
            var font = (DictionaryToken)document.Structure.GetObject(reference).Data;
            var name = ((NameToken)Resolve(document, font.Data[NameToken.BaseFont.Data])).Data;
            var descendant = font.TryGet(NameToken.DescendantFonts, out var descendants) && Resolve(document, descendants) is ArrayToken array
                ? (DictionaryToken)Resolve(document, array.Data[0])
                : font;
            var descriptor = (DictionaryToken)Resolve(document, descendant.Data[NameToken.FontDescriptor.Data]);
            var embedded = descriptor.ContainsKey(NameToken.FontFile2) || descriptor.ContainsKey(NameToken.FontFile3) || descriptor.ContainsKey(NameToken.FontFile);
            return (name, embedded);
        })];
    }

    public static (DocumentInformation Information, string Xmp) Metadata(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        var xmp = document.TryGetXmpMetadata(out var metadata) ? Encoding.UTF8.GetString(metadata.GetXmlBytes().ToArray()) : "";
        return (document.Information, xmp);
    }

    // The file as written plus every stream inflated, so a search also sees inside compressed content.
    public static IEnumerable<byte[]> RawAndInflated(byte[] pdf)
    {
        yield return pdf;
        var marker = "stream"u8.ToArray();
        for (var offset = 0; ;)
        {
            var start = pdf.AsSpan(offset).IndexOf(marker);
            if (start < 0)
            {
                yield break;
            }

            start += offset + marker.Length;
            var end = pdf.AsSpan(start).IndexOf("endstream"u8);
            if (end < 0)
            {
                yield break;
            }

            var body = pdf.AsSpan(start, end).TrimStart("\r\n"u8).ToArray();
            offset = start + end + "endstream".Length;
            var inflated = Inflate(body);
            if (inflated is not null)
            {
                yield return inflated;
            }
        }
    }

    public static bool Contains(byte[] data, string text) =>
        data.AsSpan().IndexOf(Encoding.ASCII.GetBytes(text)) >= 0 || data.AsSpan().IndexOf(Encoding.BigEndianUnicode.GetBytes(text)) >= 0;

    private static byte[]? Inflate(byte[] body)
    {
        try
        {
            using var input = new ZLibStream(new MemoryStream(body), CompressionMode.Decompress);
            using var output = new MemoryStream();
            input.CopyTo(output);
            return output.ToArray();
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static IToken Resolve(PdfDocument document, IToken token) =>
        token is IndirectReferenceToken reference ? Resolve(document, document.Structure.GetObject(reference.Data).Data) : token;
}
