using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Generation.Output;

namespace Sipos.Resume.Generation.Assets;

/// <summary>The version of the static files a site's pages load: a hash of their contents.</summary>
/// <remarks>
/// <para>
/// Decision: one hash over every file the theme's pages load (its required assets and the files it writes), which the
/// pages append to each address as <c>?v=</c>.
/// Why: GitHub Pages lets a browser keep a file for ten minutes, so right after a deploy a returning reader got the new
/// page with the old stylesheet; the query changes only when one of the files did. One hash rather than one per file,
/// because a module passes its own query to the modules it imports, which a hash per file would leave stale.
/// </para>
/// <para>
/// Decision: the first twelve hexadecimal digits of SHA-256.
/// Why: the hash tells builds apart, it guards nothing; twelve digits keep the addresses short.
/// </para>
/// </remarks>
internal static class AssetVersion
{
    private const int Length = 12;

    /// <summary>Hashes the files the pages load, each with its site path, in the order of their paths.</summary>
    /// <param name="assetsRoot">The published web root the build copies the assets from.</param>
    /// <param name="requiredAssets">The theme's assets, as site paths; one the web root lacks is left out.</param>
    /// <param name="files">The files the theme writes for the site.</param>
    /// <returns>The version, or <see langword="null"/> when there is no file to hash.</returns>
    public static string? Of(string assetsRoot, IEnumerable<string> requiredAssets, IEnumerable<ThemeFile> files)
    {
        var contents = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var path in requiredAssets)
        {
            var file = Path.Combine(assetsRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            // A linked file is left to the copying, which refuses it.
            if (File.Exists(file) && OutputPaths.LinkIn(file) is null)
            {
                contents[path] = File.ReadAllBytes(file);
            }
        }

        foreach (var file in files)
        {
            contents[file.Path] = Encoding.UTF8.GetBytes(file.Content);
        }

        if (contents.Count == 0)
        {
            return null;
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(long)];
        foreach (var (path, content) in contents)
        {
            // Each file as its path, its length and its bytes, so no two sets of files run together into the same input.
            hash.AppendData(Encoding.UTF8.GetBytes(path));
            BinaryPrimitives.WriteInt64LittleEndian(length, content.Length);
            hash.AppendData(length);
            hash.AppendData(content);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset())[..Length];
    }
}
