using System.Security.Cryptography;
using System.Text;

namespace Sipos.Resume.Documents.Pdf.Layouts;

/// <summary>Replaces the random identifier QuestPDF stamps on a PDF with one derived from the file's content.</summary>
/// <remarks>
/// <para>
/// Decision: after QuestPDF writes the file, its identifier (the trailer's <c>/ID</c> and the XMP
/// <c>xmpMM:DocumentID</c> and <c>xmpMM:InstanceID</c>) is overwritten in place with a hash of the rest of the file.
/// Why: Skia, under QuestPDF, makes the identifier from the clock, so two builds of the same content differ in those
/// 32 hex digits and nothing else, and the site would publish a changed PDF on every build. The identifier has a
/// fixed length, so replacing it moves no byte offset and the cross-reference table stays valid; the PDF
/// specification asks only that the identifier tell different files apart, which a content hash does.
/// Considered: accepting different bytes and comparing text and metadata instead, which keeps every build's downloads
/// changing; post-processing with a PDF library, a second dependency for one field.
/// </para>
/// <para>When the file does not hold the identifier in the expected form, as another QuestPDF version may write it, the file stays as written.</para>
/// </remarks>
internal static class ReproducibleId
{
    private const int UuidLength = 36;
    private static readonly byte[] DocumentIdTag = "<xmpMM:DocumentID>uuid:"u8.ToArray();

    /// <summary>Rewrites the identifier of a PDF in place; returns whether it did.</summary>
    /// <param name="pdf">The PDF's bytes.</param>
    public static bool Apply(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        var tag = pdf.AsSpan().IndexOf(DocumentIdTag);
        if (tag < 0 || tag + DocumentIdTag.Length + UuidLength > pdf.Length)
        {
            return false;
        }

        var uuid = Encoding.ASCII.GetString(pdf, tag + DocumentIdTag.Length, UuidLength);
        if (!Guid.TryParseExact(uuid, "D", out _))
        {
            return false;
        }

        // The XMP writes the identifier as a lowercase UUID, the trailer as 32 uppercase hex digits; each appears twice.
        byte[][] forms = [Encoding.ASCII.GetBytes(uuid), Encoding.ASCII.GetBytes(uuid.Replace("-", "", StringComparison.Ordinal).ToUpperInvariant())];
        var places = forms.Select(form => Occurrences(pdf, form)).ToList();
        if (places.Any(found => found.Count < 2))
        {
            return false;
        }

        for (var form = 0; form < forms.Length; form++)
        {
            foreach (var index in places[form])
            {
                pdf.AsSpan(index, forms[form].Length).Fill((byte)'0');
            }
        }

        var hash = SHA256.HashData(pdf)[..16];
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        var hex = Convert.ToHexString(hash);
        byte[][] replacements =
        [
            Encoding.ASCII.GetBytes($"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}".ToLowerInvariant()),
            Encoding.ASCII.GetBytes(hex),
        ];
        for (var form = 0; form < forms.Length; form++)
        {
            foreach (var index in places[form])
            {
                replacements[form].CopyTo(pdf, index);
            }
        }

        return true;
    }

    private static List<int> Occurrences(byte[] data, byte[] value)
    {
        var found = new List<int>();
        for (var offset = 0; ;)
        {
            var index = data.AsSpan(offset).IndexOf(value);
            if (index < 0)
            {
                return found;
            }

            found.Add(offset + index);
            offset += index + value.Length;
        }
    }
}
