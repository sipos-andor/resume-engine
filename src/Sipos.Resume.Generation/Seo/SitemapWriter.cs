using System.Globalization;
using System.Text;
using System.Xml;
using Sipos.Resume.Core.Site;

namespace Sipos.Resume.Generation.Seo;

/// <summary>Writes <c>sitemap.xml</c>: every language's page with its last change and the alternates of all languages.</summary>
/// <remarks>
/// Decision: each URL names every language version, itself included, and <c>x-default</c> as the root, with
/// <c>xhtml:link</c> elements, as the pages' own <c>hreflang</c> links do.
/// Why: search engines take the alternates from either place, and Google asks for the same set in both, each version
/// listing itself too.
/// </remarks>
internal static class SitemapWriter
{
    private const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private const string XhtmlNamespace = "http://www.w3.org/1999/xhtml";

    /// <summary>Returns the sitemap of a site's pages.</summary>
    /// <param name="pages">The pages, the default language first.</param>
    public static string Write(IReadOnlyList<SitePage> pages)
    {
        var text = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false), OmitXmlDeclaration = false };
        using (var xml = XmlWriter.Create(new StringWriterWithEncoding(text), settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("urlset", SitemapNamespace);
            xml.WriteAttributeString("xmlns", "xhtml", null, XhtmlNamespace);
            foreach (var page in pages)
            {
                xml.WriteStartElement("url", SitemapNamespace);
                xml.WriteElementString("loc", SitemapNamespace, page.Settings.Url(page.Path).AbsoluteUri);
                if (page.Document.LastModified is { } modified)
                {
                    xml.WriteElementString("lastmod", SitemapNamespace, modified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                foreach (var (hrefLang, path) in Alternates(pages))
                {
                    xml.WriteStartElement("xhtml", "link", XhtmlNamespace);
                    xml.WriteAttributeString("rel", "alternate");
                    xml.WriteAttributeString("hreflang", hrefLang);
                    xml.WriteAttributeString("href", page.Settings.Url(path).AbsoluteUri);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            xml.WriteEndElement();
            xml.WriteEndDocument();
        }

        return text.ToString() + "\n";
    }

    /// <summary>The alternates of the site's pages: every language by its tag, then <c>x-default</c> for the root.</summary>
    /// <param name="pages">The pages, the default language first.</param>
    public static IEnumerable<(string HrefLang, string Path)> Alternates(IReadOnlyList<SitePage> pages) =>
        pages.Select(page => (page.Document.Language.Tag, page.Path))
            .Append(("x-default", pages.First(page => page.Document.Language.IsDefault).Path));

    private sealed class StringWriterWithEncoding(StringBuilder builder) : StringWriter(builder, CultureInfo.InvariantCulture)
    {
        public override Encoding Encoding => new UTF8Encoding(false);
    }
}
