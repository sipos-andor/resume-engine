namespace Sipos.Resume.Generation.Output;

/// <summary>Checks and maps the site paths the build writes.</summary>
internal static class SitePaths
{
    /// <summary>
    /// Throws unless a path is absolute within the site and stays inside it: no empty, <c>.</c> or <c>..</c> segment,
    /// and no <c>:</c>, which on Windows names a drive (<c>C:</c>) or a file's stream.
    /// </summary>
    /// <param name="path">A site path, such as <c>/hu/index.html</c>.</param>
    public static void Check(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path[0] != '/' || path.EndsWith('/') || path.Contains('\\', StringComparison.Ordinal) ||
            path.Split('/').Skip(1).Any(segment => segment is "" or "." or ".." || segment.Contains(':', StringComparison.Ordinal)))
        {
            throw new ArgumentException($"'{path}' is not a file path within the site.", nameof(path));
        }
    }

    /// <summary>The file a page's path is published as: <c>/hu/</c> is <c>/hu/index.html</c>.</summary>
    /// <param name="pagePath">A page path ending with <c>/</c>.</param>
    public static string IndexOf(string pagePath) => pagePath + "index.html";
}
