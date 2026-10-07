using Sipos.Resume.Generation.Seo;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Seo;

public class RobotsWriterWrite
{
    [Fact]
    public void AllowsEveryCrawlerAndNamesSitemapGivenSettings() =>
        RobotsWriter.Write(Pages.Settings).ShouldBe("User-agent: *\nAllow: /\n\nSitemap: https://cv.example.com/sitemap.xml\n");
}
