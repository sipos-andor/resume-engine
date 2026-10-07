using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Core.Validation;
using Sipos.Resume.Generation.Building;
using Sipos.Resume.Generation.Output;
using Sipos.Resume.Generation.Seo;

namespace Sipos.Resume.Generation.Verification;

/// <summary>A problem in the written site.</summary>
/// <param name="Path">The site path of the file, such as <c>/hu/index.html</c>.</param>
/// <param name="Message">What is wrong.</param>
public sealed record OutputIssue(string Path, string Message)
{
    /// <summary>Returns the issue as one line.</summary>
    public override string ToString() => $"{Path}: {Message}";
}

/// <summary>
/// Checks the written site before it is published: every page, download and machine-readable file is there, every
/// local link resolves, the pages declare their language, alternates and policy, and no file carries an e-mail address
/// as text.
/// </summary>
/// <remarks>
/// <para>
/// Decision: the build reads back what it wrote and fails on any issue.
/// Why: the parts are written by different writers and a third-party theme; only the finished folder shows a broken
/// link, a page without its language or an address in a file, and a deployed mistake is public.
/// </para>
/// <para>
/// Decision: the e-mail check covers every text file and every part of every DOCX by pattern, and every file, PDFs
/// included, for the configured address itself.
/// Why: the address is allowed in one place only, as an image inside the PDFs; a PDF's text is compressed, so the
/// PDF writer's own tests read its text layer, and this check is the last net.
/// </para>
/// </remarks>
internal static class OutputVerifier
{
    /// <summary>The largest file the site may hold.</summary>
    public const long MaxFileBytes = 1024 * 1024;

    private static readonly string[] TextExtensions = [".html", ".md", ".txt", ".json", ".xml", ".js", ".mjs", ".css", ".svg", ".webmanifest"];

    /// <summary>Checks a written site.</summary>
    /// <param name="root">The output folder.</param>
    /// <param name="pages">The pages the build wrote, the default language first.</param>
    /// <param name="requiredAssets">The theme's assets the pages reference.</param>
    /// <param name="contactEmail">The e-mail address the PDFs may show as an image, or <see langword="null"/>.</param>
    public static IReadOnlyList<OutputIssue> Verify(string root, IReadOnlyList<SitePage> pages, IReadOnlyList<string> requiredAssets, string? contactEmail)
    {
        ArgumentNullException.ThrowIfNull(pages);
        var issues = new List<OutputIssue>();
        var site = new SiteFolder(root);
        var settings = pages[0].Settings;

        var expected = new List<string> { SiteBuilder.NotFoundPath, "/sitemap.xml", "/robots.txt", LlmsTxtWriter.FullPath };
        foreach (var page in pages)
        {
            expected.AddRange([SitePaths.IndexOf(page.Path), page.MarkdownPath, page.JsonResumePath, page.LlmsTxtPath]);
            expected.AddRange(page.Downloads.Select(download => download.Path));
            if (page.ShareImagePath is { } image)
            {
                expected.Add(image);
            }
        }

        expected.AddRange(requiredAssets.Select(asset => asset.StartsWith('/') ? asset : "/" + asset));
        issues.AddRange(expected.Distinct().Where(path => !site.Exists(path)).Select(path => new OutputIssue(path, "Is missing.")));

        var alternates = SitemapWriter.Alternates(pages).Select(alternate => (alternate.HrefLang, settings.Url(alternate.Path).AbsoluteUri)).ToHashSet();
        var policy = ContentSecurityPolicy.For(settings);
        foreach (var page in pages.Where(page => site.Exists(SitePaths.IndexOf(page.Path))))
        {
            var path = SitePaths.IndexOf(page.Path);
            var document = new HtmlParser().ParseDocument(site.ReadText(path));
            CheckPage(issues, path, document, policy, site, settings);
            if (document.DocumentElement.GetAttribute("lang") != page.Document.Language.Tag)
            {
                issues.Add(new OutputIssue(path, $"<html lang> must be '{page.Document.Language.Tag}'."));
            }

            var canonical = document.QuerySelectorAll("link[rel=canonical]").Select(link => link.GetAttribute("href")).ToList();
            if (canonical.Count != 1 || canonical[0] != settings.Url(page.Path).AbsoluteUri)
            {
                issues.Add(new OutputIssue(path, $"Must have one canonical link to {settings.Url(page.Path).AbsoluteUri}."));
            }

            var links = document.QuerySelectorAll("link[rel=alternate][hreflang]").Select(link => (link.GetAttribute("hreflang")!, link.GetAttribute("href")!)).ToHashSet();
            if (!links.SetEquals(alternates))
            {
                issues.Add(new OutputIssue(path, "Its hreflang links must name every language and x-default, as the sitemap does."));
            }
        }

        if (site.Exists(SiteBuilder.NotFoundPath))
        {
            var document = new HtmlParser().ParseDocument(site.ReadText(SiteBuilder.NotFoundPath));
            CheckPage(issues, SiteBuilder.NotFoundPath, document, policy, site, settings);
            if (document.QuerySelector("meta[name=robots]")?.GetAttribute("content")?.Contains("noindex", StringComparison.Ordinal) != true)
            {
                issues.Add(new OutputIssue(SiteBuilder.NotFoundPath, "Must ask crawlers not to index it."));
            }
        }

        CheckSitemap(issues, site, pages);
        foreach (var page in pages.Where(page => site.Exists(page.LlmsTxtPath)))
        {
            CheckLinks(issues, page.LlmsTxtPath, MarkdownLinks(site.ReadText(page.LlmsTxtPath)), site, settings);
        }

        foreach (var path in site.Files())
        {
            CheckFile(issues, site, path, contactEmail);
        }

        return issues;
    }

    private static void CheckPage(List<OutputIssue> issues, string path, IDocument document, string policy, SiteFolder site, SiteSettings settings)
    {
        var policies = document.QuerySelectorAll("meta[http-equiv='Content-Security-Policy']").Select(meta => meta.GetAttribute("content")).ToList();
        if (policies.Count != 1 || policies[0] != policy)
        {
            issues.Add(new OutputIssue(path, "Must carry exactly the site's Content-Security-Policy meta element."));
        }

        foreach (var script in document.Scripts.Where(script => script.Source is null or ""))
        {
            if (script.Type is not ("application/ld+json" or "application/json"))
            {
                issues.Add(new OutputIssue(path, "Inline scripts are not allowed; only JSON data blocks may be inline."));
            }
        }

        // Script also runs from an event handler attribute and a javascript: address, which the policy blocks too.
        foreach (var element in document.All)
        {
            if (element.Attributes.FirstOrDefault(attribute => attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)) is { } handler)
            {
                issues.Add(new OutputIssue(path, $"Inline scripts are not allowed; <{element.LocalName}> has the event handler {handler.Name}."));
            }

            if (new[] { element.GetAttribute("href"), element.GetAttribute("src") }.Any(address => address?.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) == true))
            {
                issues.Add(new OutputIssue(path, $"Inline scripts are not allowed; <{element.LocalName}> links to a javascript: address."));
            }
        }

        // A srcset lists candidates, each an address and a width or density, separated by commas.
        var references = document.QuerySelectorAll("a[href], link[href], script[src], img[src]")
            .Select(element => element.GetAttribute("href") ?? element.GetAttribute("src"))
            .Concat(document.QuerySelectorAll("img[srcset], source[srcset]")
                .SelectMany(element => element.GetAttribute("srcset")!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(candidate => candidate.Split(' ', 2)[0]))
            .OfType<string>();
        var baseHref = document.QuerySelector("base[href]")?.GetAttribute("href");
        CheckLinks(issues, path, references.Select(reference => Resolve(path, baseHref, reference)), site, settings);
    }

    private static void CheckSitemap(List<OutputIssue> issues, SiteFolder site, IReadOnlyList<SitePage> pages)
    {
        if (!site.Exists("/sitemap.xml"))
        {
            return;
        }

        XNamespace sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locations = XDocument.Parse(site.ReadText("/sitemap.xml")).Root!.Elements(sitemap + "url").Select(url => url.Element(sitemap + "loc")?.Value).ToHashSet();
        if (!locations.SetEquals(pages.Select(page => page.Settings.Url(page.Path).AbsoluteUri)))
        {
            issues.Add(new OutputIssue("/sitemap.xml", "Must list exactly the language pages."));
        }
    }

    private static void CheckLinks(List<OutputIssue> issues, string path, IEnumerable<string> references, SiteFolder site, SiteSettings settings)
    {
        foreach (var reference in references.Distinct())
        {
            if (LocalPath(reference, settings) is { } local && !site.Exists(local.EndsWith('/') ? SitePaths.IndexOf(local) : local))
            {
                issues.Add(new OutputIssue(path, $"Links to {reference}, which the site does not have."));
            }
        }
    }

    private static void CheckFile(List<OutputIssue> issues, SiteFolder site, string path, string? contactEmail)
    {
        var bytes = site.ReadBytes(path);
        if (bytes.Length > MaxFileBytes)
        {
            issues.Add(new OutputIssue(path, $"Is {bytes.Length / 1024} KB; files must stay under {MaxFileBytes / 1024} KB."));
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (TextExtensions.Contains(extension) && EmailGuard.ContainsAddress(Encoding.UTF8.GetString(bytes)))
        {
            issues.Add(new OutputIssue(path, "Contains an e-mail address; only the PDFs may show one, as an image."));
        }
        else if (extension == ".docx" && DocxText(bytes).Any(EmailGuard.ContainsAddress))
        {
            issues.Add(new OutputIssue(path, "Contains an e-mail address; only the PDFs may show one, as an image."));
        }

        if (contactEmail is { Length: > 0 } email && Contains(bytes, email))
        {
            issues.Add(new OutputIssue(path, "Contains the contact e-mail address as text; it may appear only as an image in the PDFs."));
        }
    }

    private static IEnumerable<string> DocxText(byte[] bytes)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        foreach (var entry in zip.Entries.Where(entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase)))
        {
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            yield return reader.ReadToEnd();
        }
    }

    private static bool Contains(byte[] bytes, string text)
    {
        foreach (var encoding in new Encoding[] { Encoding.UTF8, Encoding.BigEndianUnicode, Encoding.Unicode })
        {
            foreach (var variant in new[] { text, text.ToLowerInvariant(), text.ToUpperInvariant() }.Distinct())
            {
                if (bytes.AsSpan().IndexOf(encoding.GetBytes(variant)) >= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // A relative address resolves against the page's <base>, or without one against the page's own folder.
    private static string Resolve(string documentPath, string? baseHref, string reference)
    {
        if (reference.Contains(':', StringComparison.Ordinal) || reference.StartsWith('/') || reference.StartsWith('#'))
        {
            return reference;
        }

        var basePath = baseHref?.StartsWith('/') == true ? baseHref : documentPath[..(documentPath.LastIndexOf('/') + 1)];
        return new Uri(new Uri("https://site.invalid" + basePath), reference).PathAndQuery;
    }

    private static string? LocalPath(string reference, SiteSettings settings)
    {
        var path = reference;
        if (!path.StartsWith('/'))
        {
            var origin = settings.Origin;
            if (!Uri.TryCreate(reference, UriKind.Absolute, out var uri)
                || !string.Equals(uri.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(uri.Host, origin.Host, StringComparison.OrdinalIgnoreCase)
                || uri.Port != origin.Port)
            {
                return null;
            }

            path = uri.AbsolutePath;
        }

        if (!path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        var end = path.IndexOfAny(['?', '#']);
        return Uri.UnescapeDataString(end < 0 ? path : path[..end]);
    }

    private static IEnumerable<string> MarkdownLinks(string text)
    {
        for (var start = text.IndexOf("](", StringComparison.Ordinal); start >= 0; start = text.IndexOf("](", start + 2, StringComparison.Ordinal))
        {
            var end = text.IndexOf(')', start);
            if (end > start)
            {
                yield return text[(start + 2)..end];
            }
        }
    }

    private sealed class SiteFolder(string root)
    {
        private readonly string _root = Path.GetFullPath(root);

        public bool Exists(string path) => File.Exists(Physical(path));

        public string ReadText(string path) => File.ReadAllText(Physical(path), Encoding.UTF8);

        public byte[] ReadBytes(string path) => File.ReadAllBytes(Physical(path));

        public IEnumerable<string> Files() =>
            Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
                .Select(file => "/" + Path.GetRelativePath(_root, file).Replace(Path.DirectorySeparatorChar, '/'))
                .Order(StringComparer.Ordinal);

        private string Physical(string path) => Path.Combine(_root, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    }
}
