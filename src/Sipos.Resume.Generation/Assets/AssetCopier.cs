using Sipos.Resume.Generation.Output;

namespace Sipos.Resume.Generation.Assets;

/// <summary>Copies a published web root's static assets into the site.</summary>
/// <remarks>
/// Decision: the assets come from the host's published <c>wwwroot</c>, where publishing laid out every package's
/// <c>_content/{package}/</c> files; the build copies them as they are, without the precompressed copies.
/// Why: the .NET SDK already knows which assets each package ships and where they belong; GitHub Pages compresses on
/// its own and would serve a <c>.br</c> file as a download.
/// Considered: reading the static web assets manifest, which describes the same files in a format that changes
/// between SDK versions.
/// </remarks>
internal static class AssetCopier
{
    private static readonly string[] SkippedExtensions = [".br", ".gz"];

    /// <summary>Copies every file of a folder, keeping its relative path as the site path.</summary>
    /// <param name="folder">The web root.</param>
    /// <param name="sink">The site.</param>
    /// <param name="cancellationToken">Stops the copying.</param>
    /// <returns>The number of files copied.</returns>
    public static async Task<int> CopyAsync(string folder, IArtifactSink sink, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folder))
        {
            return 0;
        }

        CheckPath(folder);
        var enumeration = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = false,
        };
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(folder, "*", enumeration).Order(StringComparer.Ordinal))
        {
            if (SkippedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var path = "/" + Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/');
            CheckPath(file);
            await sink.WriteAsync(path, await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            count++;
        }

        return count;
    }

    // Enumeration skips directory and file links, including cycles. Recheck before reading in case a discovered
    // file or one of its parents has since become a link.
    private static void CheckPath(string path)
    {
        if (OutputPaths.LinkIn(path) is { } link)
        {
            throw new IOException($"Asset path traverses a symbolic link or junction ({link}).");
        }
    }
}
