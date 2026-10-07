using Microsoft.Extensions.Logging.Abstractions;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests;

public class ResumeGeneratorRun
{
    private static ResumeGenerator Generator(TempFolder folder, IReadOnlyDictionary<string, string>? environment = null, params string[] extra) =>
        ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"), "--output", Path.Combine(folder.Path, "dist"), "--assets", Path.Combine(folder.Path, "wwwroot"), "--today", "2026-10-07", .. extra])
            .UseTheme(new FakeTheme())
            .UseLogging(NullLoggerFactory.Instance)
            .UseEnvironment(name => environment?.GetValueOrDefault(name));

    [Fact]
    public async Task BuildsAndVerifiesSiteGivenValidContent()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        Sites.WriteAssets(folder);

        (await Generator(folder).RunAsync(TestContext.Current.CancellationToken)).ShouldBe(0);

        folder.Exists("dist/hu/index.html").ShouldBeTrue();
    }

    [Fact]
    public async Task FailsWithoutWritingGivenContentIssues()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        folder.Write("content/resume.hu.json", Samples.Hungarian.Replace("\"2015-03\"", "\"2016-03\"", StringComparison.Ordinal));

        (await Generator(folder).RunAsync(TestContext.Current.CancellationToken)).ShouldBe(1);

        folder.Exists("dist/index.html").ShouldBeFalse();
    }

    [Fact]
    public async Task ChecksOnlyGivenValidateOnly()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);

        (await Generator(folder, null, "--validate-only").RunAsync(TestContext.Current.CancellationToken)).ShouldBe(0);

        Directory.Exists(Path.Combine(folder.Path, "dist")).ShouldBeFalse();
    }

    // A mistyped --output must not delete someone's folder.
    [Fact]
    public async Task RefusesFolderWithFilesGivenNoClean()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        Sites.WriteAssets(folder);
        folder.Write("dist/keep.txt", "mine");

        (await Generator(folder).RunAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        folder.Exists("dist/keep.txt").ShouldBeTrue();

        (await Generator(folder, null, "--clean").RunAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        folder.Exists("dist/keep.txt").ShouldBeFalse();
    }

    // An issue may quote a value, such as a parity difference; the address must not reach the build log.
    [Fact]
    public async Task LeavesAddressOutOfLogGivenAddressInContent()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        folder.Write("content/resume.en.json", Samples.English.Replace("https://github.com/ann", "mailto:ann@example.org", StringComparison.Ordinal));
        var logging = new RecordingLoggerFactory();

        var code = await ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"), "--validate-only"])
            .UseLogging(logging)
            .RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(1);
        logging.Lines.ShouldContain(line => line.Contains("/basics/profiles/0/url", StringComparison.Ordinal));
        logging.Lines.ShouldAllBe(line => !line.Contains("ann@", StringComparison.Ordinal) && !line.Contains("mailto:ann", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FailsGivenRequiredEmailNotSet()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);

        (await Generator(folder, null, "--require-email").RunAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task AllowsAnalyticsHostsGivenTokenInEnvironment()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        Sites.WriteAssets(folder);

        var code = await Generator(folder, new Dictionary<string, string> { [ResumeGenerator.AnalyticsTokenVariable] = "token" }).RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(0);
        folder.Read("dist/index.html").ShouldContain("https://static.cloudflareinsights.com");
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--today", "07.10.2026")]
    [InlineData("--content")]
    public async Task ReturnsUsageErrorGivenBadArguments(params string[] args)
    {
        var code = await ResumeGenerator.Create(args).UseTheme(new FakeTheme()).UseLogging(NullLoggerFactory.Instance).RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(2);
    }

    [Fact]
    public async Task TakesPresentFromSourceDateEpochGivenNoToday()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);

        var code = await ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"), "--validate-only"])
            .UseLogging(NullLoggerFactory.Instance)
            .UseEnvironment(name => name == ResumeGenerator.SourceDateEpochVariable ? "not-a-number" : null)
            .RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(2);
    }
}
