using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sipos.Resume.Documents.Pdf.Layouts;

/// <summary>Replaces the random identifier QuestPDF stamps on a PDF with one derived from the file's content.</summary>
/// <remarks>
/// <para>
/// Decision: after QuestPDF writes the file, its identifier (the XMP <c>xmpMM:DocumentID</c> and
/// <c>xmpMM:InstanceID</c>, and the trailer's <c>/ID</c>) is replaced with a hash of the rest of the file.
/// Why: Skia, under QuestPDF, makes the identifier from the clock, so two builds of the same content differ in it and
/// nothing else, and the site would publish a changed PDF on every build. The PDF specification asks only that the
/// identifier tell different files apart, which a content hash does.
/// Considered: accepting different bytes and comparing text and metadata instead, which keeps every build's downloads
/// changing; post-processing with a PDF library, a second dependency for one field.
/// </para>
/// <para>
/// The XMP UUIDs have a fixed length and are overwritten in place. Skia writes the trailer's two strings as hex
/// (<c>&lt;…&gt;</c>) or, when that is shorter, as literals (<c>(…)</c>) with escapes, so the <c>/ID</c> array is
/// parsed and written again as hex. The trailer follows the cross-reference table that <c>startxref</c> points to, so
/// a new length there moves no recorded offset.
/// </para>
/// </remarks>
internal static class ReproducibleId
{
    private const int UuidLength = 36;
    private static readonly byte[] DocumentIdTag = "<xmpMM:DocumentID>uuid:"u8.ToArray();

    /// <summary>Returns the PDF with a reproducible identifier, or <see langword="null"/> when it is not in the form Skia writes.</summary>
    /// <param name="pdf">The PDF's bytes, which stay as they are.</param>
    public static byte[]? Apply(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        var tag = pdf.AsSpan().IndexOf(DocumentIdTag);
        if (tag < 0 || tag + DocumentIdTag.Length + UuidLength > pdf.Length)
        {
            return null;
        }

        var uuid = Encoding.ASCII.GetString(pdf, tag + DocumentIdTag.Length, UuidLength);
        if (!Guid.TryParseExact(uuid, "D", out _))
        {
            return null;
        }

        // The XMP writes the identifier twice, as the document's and the instance's.
        var places = Occurrences(pdf, Encoding.ASCII.GetBytes(uuid));
        if (places.Count < 2 || TrailerIdArray(pdf) is not { } span || places.Any(place => place + UuidLength > span.Start))
        {
            return null;
        }

        var (start, end) = span;

        var zeros = new string('0', 32);
        var array = Encoding.ASCII.GetBytes($"[<{zeros}> <{zeros}>]");
        var file = new byte[start + array.Length + (pdf.Length - end)];
        pdf.AsSpan(0, start).CopyTo(file);
        array.CopyTo(file, start);
        pdf.AsSpan(end).CopyTo(file.AsSpan(start + array.Length));
        foreach (var place in places)
        {
            file.AsSpan(place, UuidLength).Fill((byte)'0');
        }

        var hash = SHA256.HashData(file)[..16];
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        var hex = Convert.ToHexString(hash);
        var id = Encoding.ASCII.GetBytes($"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}".ToLowerInvariant());
        foreach (var place in places)
        {
            id.CopyTo(file, place);
        }

        Encoding.ASCII.GetBytes($"[<{hex}> <{hex}>]").CopyTo(file, start);
        return file;
    }

    // The span of the last trailer's /ID array, from its "[" to after its "]", when the trailer follows the
    // cross-reference table that startxref names.
    private static (int Start, int End)? TrailerIdArray(byte[] pdf)
    {
        var trailer = pdf.AsSpan().LastIndexOf("trailer"u8);
        var startxref = pdf.AsSpan().LastIndexOf("startxref"u8);
        if (trailer < 0 || startxref < trailer || !int.TryParse(Encoding.ASCII.GetString(pdf, startxref + 9, Math.Min(24, pdf.Length - startxref - 9)).Split((char[])['\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var xref) || xref >= trailer)
        {
            return null;
        }

        var key = pdf.AsSpan(trailer, startxref - trailer).IndexOf("/ID"u8);
        if (key < 0)
        {
            return null;
        }

        var start = SkipSpace(pdf, trailer + key + 3);
        if (start >= pdf.Length || pdf[start] != '[')
        {
            return null;
        }

        var position = start + 1;
        for (var item = 0; item < 2; item++)
        {
            position = SkipSpace(pdf, position);
            position = EndOfString(pdf, position);
            if (position < 0)
            {
                return null;
            }
        }

        position = SkipSpace(pdf, position);
        return position < pdf.Length && pdf[position] == ']' ? (start, position + 1) : null;
    }

    // The position after a hex string <…> or a literal string (…) that starts at the position, or -1.
    private static int EndOfString(byte[] pdf, int position)
    {
        if (position >= pdf.Length)
        {
            return -1;
        }

        if (pdf[position] == '<')
        {
            var close = pdf.AsSpan(position).IndexOf((byte)'>');
            return close < 0 ? -1 : position + close + 1;
        }

        if (pdf[position] != '(')
        {
            return -1;
        }

        // A literal string balances its parentheses, and a backslash escapes the byte after it.
        var depth = 0;
        for (var index = position; index < pdf.Length; index++)
        {
            switch (pdf[index])
            {
                case (byte)'\\':
                    index++;
                    break;
                case (byte)'(':
                    depth++;
                    break;
                case (byte)')' when --depth == 0:
                    return index + 1;
            }
        }

        return -1;
    }

    private static int SkipSpace(byte[] pdf, int position)
    {
        while (position < pdf.Length && pdf[position] is (byte)' ' or (byte)'\r' or (byte)'\n' or (byte)'\t')
        {
            position++;
        }

        return position;
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
