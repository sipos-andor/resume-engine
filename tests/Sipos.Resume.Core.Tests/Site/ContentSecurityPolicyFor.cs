using Sipos.Resume.Core.Site;

namespace Sipos.Resume.Core.Tests.Site;

public class ContentSecurityPolicyFor
{
    private static readonly SiteSettings Settings = new(new Uri("https://cv.example.com"), "en", [], "Ann_CV", "cv-theme", "cv-language", null);

    [Fact]
    public void AllowsOnlyOwnScriptsGivenNoAnalytics()
    {
        var policy = ContentSecurityPolicy.For(Settings);

        policy.ShouldContain("script-src 'self';");
        policy.ShouldContain("connect-src 'self';");
        policy.ShouldNotContain("cloudflareinsights");
        policy.ShouldNotContain("unsafe-eval");
    }

    [Fact]
    public void AllowsBeaconHostsGivenAnalyticsToken()
    {
        var policy = ContentSecurityPolicy.For(Settings with { AnalyticsToken = "token" });

        policy.ShouldContain("script-src 'self' https://static.cloudflareinsights.com;");
        policy.ShouldContain("connect-src 'self' https://cloudflareinsights.com;");
    }

    // Inline scripts stay blocked: the pages' JSON blocks are data, which the policy does not govern.
    [Fact]
    public void BlocksInlineScriptsAndPluginsGivenAnySettings()
    {
        var directives = ContentSecurityPolicy.For(Settings).Split("; ");

        directives.Single(directive => directive.StartsWith("script-src", StringComparison.Ordinal)).ShouldNotContain("unsafe-inline");
        directives.ShouldContain("object-src 'none'");
        directives.ShouldContain("base-uri 'self'");
        directives.ShouldContain("form-action 'none'");
    }
}
