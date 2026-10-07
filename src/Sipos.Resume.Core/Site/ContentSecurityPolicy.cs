namespace Sipos.Resume.Core.Site;

/// <summary>The Content-Security-Policy of a CV site's pages, which a theme writes as a <c>meta</c> element.</summary>
/// <remarks>
/// <para>
/// Decision: the policy is a <c>meta</c> element in every page, written from one place.
/// Why: GitHub Pages sends no custom headers; the build checks that every page carries exactly this policy.
/// Considered: a header from a CDN in front of Pages, which is one more service to run for a static CV.
/// </para>
/// <para>
/// Decision: scripts only from the site itself and, with analytics, from Cloudflare's beacon host; styles may be
/// inline.
/// Why: the pages run no inline script (their JSON blocks are data, which the policy does not govern); the timeline
/// places its bars with style attributes. <c>frame-ancestors</c> is left out because browsers ignore it in a
/// <c>meta</c> element.
/// </para>
/// </remarks>
public static class ContentSecurityPolicy
{
    /// <summary>The host Cloudflare Web Analytics loads its beacon from.</summary>
    public const string AnalyticsScriptOrigin = "https://static.cloudflareinsights.com";

    /// <summary>The host the beacon reports to.</summary>
    public const string AnalyticsReportOrigin = "https://cloudflareinsights.com";

    /// <summary>Returns the policy of a site's pages.</summary>
    /// <param name="settings">The site's settings; with an analytics token the beacon's hosts are allowed.</param>
    public static string For(SiteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var analytics = settings.AnalyticsToken is not null;
        return string.Join("; ",
            "default-src 'self'",
            analytics ? $"script-src 'self' {AnalyticsScriptOrigin}" : "script-src 'self'",
            analytics ? $"connect-src 'self' {AnalyticsReportOrigin}" : "connect-src 'self'",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data:",
            "font-src 'self'",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'none'");
    }
}
