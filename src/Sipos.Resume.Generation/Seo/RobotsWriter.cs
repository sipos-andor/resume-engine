using Sipos.Resume.Core.Site;

namespace Sipos.Resume.Generation.Seo;

/// <summary>Writes <c>robots.txt</c>: every crawler may read everything, and the sitemap is named.</summary>
/// <remarks>
/// Decision: no crawler is turned away, language models' included.
/// Why: a CV is published to be found and read; the e-mail address, the one thing to keep from crawlers, is never in
/// any file they can read as text.
/// </remarks>
internal static class RobotsWriter
{
    /// <summary>Returns the robots.txt of a site.</summary>
    /// <param name="settings">The site's settings.</param>
    public static string Write(SiteSettings settings) =>
        $"User-agent: *\nAllow: /\n\nSitemap: {settings.Url("/sitemap.xml").AbsoluteUri}\n";
}
