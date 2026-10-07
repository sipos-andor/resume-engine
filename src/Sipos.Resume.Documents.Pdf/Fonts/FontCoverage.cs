using System.Buffers.Binary;

namespace Sipos.Resume.Documents.Pdf.Fonts;

/// <summary>The characters a TrueType or OpenType font has glyphs for, read from its <c>cmap</c> table.</summary>
/// <remarks>
/// Decision: read the bundled fonts' character maps instead of asking QuestPDF.
/// Why: QuestPDF reports a missing glyph only by failing while it draws, after the build has emptied the output; the
/// map lets the validation name the field and the character first. Formats 12 (every plane) and 4 (the basic plane)
/// are the ones fonts carry for Unicode.
/// </remarks>
internal static class FontCoverage
{
    /// <summary>Returns the code points the font maps to a glyph.</summary>
    /// <param name="font">The font file's bytes.</param>
    /// <exception cref="InvalidDataException">The font has no Unicode character map.</exception>
    public static HashSet<int> Of(ReadOnlySpan<byte> font)
    {
        var cmap = -1;
        for (int index = 0, tables = U16(font, 4); index < tables; index++)
        {
            var record = 12 + (index * 16);
            if (font.Slice(record, 4).SequenceEqual("cmap"u8))
            {
                cmap = (int)U32(font, record + 8);
                break;
            }
        }

        if (cmap < 0)
        {
            throw new InvalidDataException("The font has no cmap table.");
        }

        int best = -1, bestFormat = 0;
        for (int index = 0, count = U16(font, cmap + 2); index < count; index++)
        {
            var record = cmap + 4 + (index * 8);
            var (platform, encoding) = (U16(font, record), U16(font, record + 2));
            var offset = cmap + (int)U32(font, record + 4);
            var format = U16(font, offset);
            var unicode = platform == 0 || (platform == 3 && encoding is 1 or 10);
            if (unicode && format is 4 or 12 && format > bestFormat)
            {
                (best, bestFormat) = (offset, format);
            }
        }

        return bestFormat switch
        {
            12 => Format12(font, best),
            4 => Format4(font, best),
            _ => throw new InvalidDataException("The font has no Unicode character map."),
        };
    }

    private static HashSet<int> Format12(ReadOnlySpan<byte> font, int table)
    {
        var covered = new HashSet<int>();
        for (long group = 0, groups = U32(font, table + 12); group < groups; group++)
        {
            var record = table + 16 + (int)(group * 12);
            var (start, end, glyph) = (U32(font, record), U32(font, record + 4), U32(font, record + 8));
            for (var code = start; code <= end; code++)
            {
                if (glyph + (code - start) != 0)
                {
                    covered.Add((int)code);
                }
            }
        }

        return covered;
    }

    private static HashSet<int> Format4(ReadOnlySpan<byte> font, int table)
    {
        var covered = new HashSet<int>();
        var segments = U16(font, table + 6) / 2;
        var ends = table + 14;
        var starts = ends + (segments * 2) + 2;
        var deltas = starts + (segments * 2);
        var ranges = deltas + (segments * 2);
        for (var segment = 0; segment < segments; segment++)
        {
            int start = U16(font, starts + (segment * 2)), end = U16(font, ends + (segment * 2));
            var delta = (short)U16(font, deltas + (segment * 2));
            var rangeAt = ranges + (segment * 2);
            var range = U16(font, rangeAt);
            for (var code = start; code <= end && code != 0xFFFF; code++)
            {
                var glyph = range == 0 ? (code + delta) & 0xFFFF : U16(font, rangeAt + range + ((code - start) * 2)) is var mapped and not 0 ? (mapped + delta) & 0xFFFF : 0;
                if (glyph != 0)
                {
                    covered.Add(code);
                }
            }
        }

        return covered;
    }

    private static ushort U16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);

    private static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
}
