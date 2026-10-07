using System.IO.Compression;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>Rewrites a package so the same parts always give the same bytes.</summary>
/// <remarks>
/// Decision: the SDK writes the package into memory, then its parts are zipped again in a fixed order with one fixed
/// timestamp: the content's last change, or 1 January 1980 (the first date a ZIP entry can hold) when it is unknown.
/// Why: <c>System.IO.Packaging</c> stamps every entry with the current time, so two builds of the same commit would
/// publish different files, which defeats caching and makes a build impossible to verify by its hash.
/// Considered: writing the ZIP and the package's relationship parts by hand, which re-implements what the SDK
/// already gets right; the relationship identifiers, the other source of randomness, are set explicitly instead.
/// </remarks>
internal static class DocxArchive
{
    private const string ContentTypes = "[Content_Types].xml";

    /// <summary>The timestamp of a document's entries.</summary>
    /// <param name="lastModified">When the content last changed, or <see langword="null"/>.</param>
    public static DateOnly StampOf(DateOnly? lastModified) => lastModified ?? new DateOnly(1980, 1, 1);

    /// <summary>Copies the parts of a package to the output, the content types first and the rest by name.</summary>
    /// <param name="package">The package as the SDK wrote it.</param>
    /// <param name="output">Where to write the normalized package.</param>
    /// <param name="stamp">The date every entry carries.</param>
    public static void Normalize(MemoryStream package, Stream output, DateOnly stamp)
    {
        package.Position = 0;
        var parts = new List<(string Name, byte[] Bytes)>();
        using (var source = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: true))
        {
            foreach (var entry in source.Entries)
            {
                using var content = entry.Open();
                using var bytes = new MemoryStream();
                content.CopyTo(bytes);
                parts.Add((entry.FullName, bytes.ToArray()));
            }
        }

        // ZipArchive writes a seekable and a forward-only stream differently, so the bytes are made in memory first.
        using var buffer = new MemoryStream();
        using (var target = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var time = new DateTimeOffset(stamp.Year, stamp.Month, stamp.Day, 0, 0, 0, TimeSpan.Zero);
            foreach (var (name, bytes) in parts.OrderBy(part => part.Name == ContentTypes ? 0 : 1).ThenBy(part => part.Name, StringComparer.Ordinal))
            {
                var entry = target.CreateEntry(name, CompressionLevel.Optimal);
                entry.LastWriteTime = time;
                using var stream = entry.Open();
                stream.Write(bytes);
            }
        }

        buffer.Position = 0;
        buffer.CopyTo(output);
    }
}
