namespace Sipos.Resume.Generation.Output;

/// <summary>Checks and maps the site paths the build writes.</summary>
internal static class SitePaths
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// Throws unless a path is absolute within the site and stays inside it: no empty, <c>.</c> or <c>..</c> segment,
    /// and only portable ASCII filename characters, so it has the same URL and filesystem name everywhere.
    /// </summary>
    /// <param name="path">A site path, such as <c>/hu/index.html</c>.</param>
    public static void Check(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path[0] != '/' || path.EndsWith('/') || path.Contains('\\', StringComparison.Ordinal) ||
            path.Split('/').Skip(1).Any(segment => !IsPortableSegment(segment)))
        {
            throw new ArgumentException($"'{path}' is not a file path within the site.", nameof(path));
        }
    }

    private static bool IsPortableSegment(string segment)
    {
        if (segment is "" or "." or ".." || segment[^1] == '.'
            || segment.Any(character => character is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-')))
        {
            return false;
        }

        var extension = segment.IndexOf('.');
        var baseName = extension < 0 ? segment : segment[..extension];
        return !ReservedNames.Contains(baseName);
    }

    /// <summary>The file a page's path is published as: <c>/hu/</c> is <c>/hu/index.html</c>.</summary>
    /// <param name="pagePath">A page path ending with <c>/</c>.</param>
    public static string IndexOf(string pagePath) => pagePath + "index.html";
}
