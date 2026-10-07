using System.Xml.Linq;
using Sipos.Resume.Generation.Seo;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Seo;

public class SitemapWriterWrite
{
    private static readonly XNamespace Sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace Xhtml = "http://www.w3.org/1999/xhtml";

    [Fact]
    public void ListsEveryLanguagePageWithLastChangeGivenPages()
    {
        var urls = XDocument.Parse(SitemapWriter.Write(Pages.Sample())).Root!.Elements(Sitemap + "url").ToList();

        urls.Select(url => url.Element(Sitemap + "loc")!.Value).ShouldBe(["https://cv.example.com/", "https://cv.example.com/hu/"]);
        urls.ShouldAllBe(url => url.Element(Sitemap + "lastmod")!.Value == "2026-10-07");
    }

    // Google asks for the same set on every version, each listing itself, with x-default for the root.
    [Fact]
    public void NamesEveryVersionAndDefaultGivenEachPage()
    {
        var urls = XDocument.Parse(SitemapWriter.Write(Pages.Sample())).Root!.Elements(Sitemap + "url");

        foreach (var url in urls)
        {
            url.Elements(Xhtml + "link").Select(link => (link.Attribute("hreflang")!.Value, link.Attribute("href")!.Value)).ShouldBe(
            [
                ("en", "https://cv.example.com/"),
                ("hu", "https://cv.example.com/hu/"),
                ("x-default", "https://cv.example.com/"),
            ]);
        }
    }
}
