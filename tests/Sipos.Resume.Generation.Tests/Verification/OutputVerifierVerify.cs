using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Generation.Documents.Json;
using Sipos.Resume.Generation.Documents.Markdown;
using Sipos.Resume.Generation.Documents.PlainText;
using Sipos.Resume.Generation.Tests.Helpers;
using Sipos.Resume.Generation.Verification;

namespace Sipos.Resume.Generation.Tests.Verification;

public class OutputVerifierVerify
{
    private static readonly IDocumentWriter[] Writers = [new MarkdownDocumentWriter(), new PlainTextDocumentWriter(), new JsonResumeDocumentWriter()];

    private static async Task<(TempFolder Folder, Func<IReadOnlyList<OutputIssue>> Verify)> BuildAsync(IDocumentWriter[]? more = null, string? email = null)
    {
        var folder = new TempFolder();
        var pages = await Sites.BuildAsync(folder, new FakeTheme(), [.. more ?? [], .. Writers], email);
        return (folder, () => OutputVerifier.Verify(Path.Combine(folder.Path, "dist"), pages, new FakeTheme().RequiredAssets, email));
    }

    [Fact]
    public async Task FindsNothingGivenCompleteSite()
    {
        var (folder, verify) = await BuildAsync();
        using (folder)
        {
            verify().ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task ReportsBrokenLinkGivenMissingDownload()
    {
        var (folder, verify) = await BuildAsync();
        using (folder)
        {
            File.Delete(Path.Combine(folder.Path, "dist/downloads/Ann_Example_CV_HU.md"));

            verify().ShouldContain(issue => issue.Path == "/downloads/Ann_Example_CV_HU.md");
            verify().ShouldContain(issue => issue.Path == "/hu/index.html" && issue.Message.Contains("/downloads/Ann_Example_CV_HU.md", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("https://cv.example.com/missing")]
    [InlineData("HTTPS://CV.EXAMPLE.COM/missing")]
    [InlineData("HTTPS://CV.EXAMPLE.COM:443/missing?query=value#section")]
    [InlineData("https://Cv.Example.Com/%6dissing")]
    public async Task ReportsBrokenLinkGivenSameOriginAbsoluteUrl(string reference)
    {
        var (folder, verify) = await BuildAsync();
        using (folder)
        {
            folder.Write("dist/hu/index.html", folder.Read("dist/hu/index.html")
                .Replace("</body>", $"<a href=\"{reference}\">x</a></body>", StringComparison.Ordinal));

            verify().ShouldContain(issue => issue.Path == "/hu/index.html"
                && issue.Message.Contains(reference, StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("HTTPS://CV.EXAMPLE.COM:443/hu/?query=value#section")]
    [InlineData("http://cv.example.com/missing")]
    [InlineData("https://other.example.com/missing")]
    [InlineData("https://cv.example.com:444/missing")]
    [InlineData("https://cv.example.com.other.example.com/missing")]
    public async Task AcceptsLinkGivenExistingLocalPathOrDifferentOrigin(string reference)
    {
        var (folder, verify) = await BuildAsync();
        using (folder)
        {
            folder.Write("dist/hu/index.html", folder.Read("dist/hu/index.html")
                .Replace("</body>", $"<a href=\"{reference}\">x</a></body>", StringComparison.Ordinal));

            verify().ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task ReportsLanguagePolicyAndScriptGivenTamperedPage()
    {
        var (folder, verify) = await BuildAsync();
        using (folder)
        {
            var page = folder.Read("dist/hu/index.html")
                .Replace("lang=\"hu\"", "lang=\"en\"", StringComparison.Ordinal)
                .Replace("form-action &#39;none&#39;", "form-action &#39;self&#39;", StringComparison.Ordinal)
                .Replace("</body>", "<script>alert(1)</script></body>", StringComparison.Ordinal);
            folder.Write("dist/hu/index.html", page);

            var issues = verify().Where(issue => issue.Path == "/hu/index.html").Select(issue => issue.Message).ToList();
            issues.ShouldContain(message => message.Contains("lang", StringComparison.Ordinal));
            issues.ShouldContain(message => message.Contains("Content-Security-Policy", StringComparison.Ordinal));
            issues.ShouldContain(message => message.Contains("Inline scripts", StringComparison.Ordinal));
        }
    }

    // Script also runs from an event handler or a javascript: address; a srcset names several files, each checked.
    [Theory]
    [InlineData("<button onclick=\"alert(1)\">x</button>", "event handler onclick")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "javascript:")]
    [InlineData("<img srcset=\"/favicon.svg 1x, /missing-2x.png 2x\" alt=\"\">", "/missing-2x.png")]
    public async Task ReportsGivenScriptInAttributeOrMissingImageCandidate(string markup, string message)
    {
        var (folder, verify) = await BuildAsync();
        using (folder)
        {
            folder.Write("dist/hu/index.html", folder.Read("dist/hu/index.html").Replace("</body>", markup + "</body>", StringComparison.Ordinal));

            verify().ShouldContain(issue => issue.Path == "/hu/index.html" && issue.Message.Contains(message, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task ReportsIncompleteAlternatesGivenPageWithoutDefault()
    {
        var (folder, verify) = await BuildAsync();
        using (folder)
        {
            var page = folder.Read("dist/index.html");
            folder.Write("dist/index.html", page.Replace("hreflang=\"x-default\"", "hreflang=\"de\"", StringComparison.Ordinal));

            verify().ShouldContain(issue => issue.Path == "/index.html" && issue.Message.Contains("hreflang", StringComparison.Ordinal));
        }
    }

    // The last net: an address that reaches any file as text stops the deployment.
    [Fact]
    public async Task ReportsEmailGivenAddressInTextOrPdf()
    {
        var (folder, verify) = await BuildAsync([new RecordingWriter(DownloadFormat.Pdf, leakEmail: true)], email: "ann@example.com");
        using (folder)
        {
            folder.Write("dist/hu/index.md", folder.Read("dist/hu/index.md") + "\nann@example.org\n");

            var issues = verify();
            issues.ShouldContain(issue => issue.Path == "/hu/index.md" && issue.Message.Contains("e-mail", StringComparison.Ordinal));
            issues.ShouldContain(issue => issue.Path == "/downloads/Ann_Example_CV_EN.pdf" && issue.Message.Contains("contact e-mail", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task ReportsNotFoundPageGivenIndexablePage()
    {
        var (folder, verify) = await BuildAsync();
        using (folder)
        {
            folder.Write("dist/404.html", folder.Read("dist/404.html").Replace("noindex", "index", StringComparison.Ordinal));

            verify().ShouldContain(issue => issue.Path == "/404.html");
        }
    }
}
