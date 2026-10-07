using Microsoft.Extensions.Logging.Abstractions;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests;

public class ResumeGeneratorRun
{
    [Theory]
    [InlineData("embedded-credentials")]
    [InlineData("long-language-path")]
    public async Task RejectsInvalidContentBeforeCleaningOutput(string kind)
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        var json = kind == "embedded-credentials"
            ? Samples.Hungarian.Replace("Builds .NET systems.", "See https://probe-user:probe-password@example.com for details", StringComparison.Ordinal)
            : Samples.Hungarian.Replace("\"meta\": {", $"\"meta\": {{ \"x-path\": \"{new string('a', 256)}\",", StringComparison.Ordinal);
        folder.Write("content/resume.hu.json", json);
        folder.Write("dist/keep.txt", "mine");
        var logging = new RecordingLoggerFactory();

        var code = await Generator(folder, null, "--clean").UseLogging(logging).RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(1);
        folder.Read("dist/keep.txt").ShouldBe("mine");
        logging.Lines.ShouldNotContain(line => line.Contains("probe-password", StringComparison.Ordinal));
        logging.Lines.ShouldNotContain(line => line.Contains("probe-user", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefusesCleanGivenLinkedContentInsideOutput(bool linkedParent)
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        Sites.WriteAssets(folder);
        folder.Write("dist/content/site.json", folder.Read("content/site.json"));
        folder.Write("dist/content/resume.en.json", folder.Read("content/resume.en.json"));
        folder.Write("dist/content/resume.hu.json", folder.Read("content/resume.hu.json"));
        folder.Write("dist/keep.txt", "mine");
        var link = Path.Combine(folder.Path, "linked-content");
        DirectoryLink.Create(link, linkedParent ? Path.Combine(folder.Path, "dist") : Path.Combine(folder.Path, "dist", "content"));
        try
        {
            var content = linkedParent ? Path.Combine(link, "content") : link;
            var code = await ResumeGenerator.Create(["--content", content, "--output", Path.Combine(folder.Path, "dist"),
                "--assets", Path.Combine(folder.Path, "wwwroot"), "--clean"])
                .UseTheme(new FakeTheme()).UseLogging(NullLoggerFactory.Instance).UseEnvironment(_ => null)
                .RunAsync(TestContext.Current.CancellationToken);

            code.ShouldBe(2);
            folder.Read("dist/keep.txt").ShouldBe("mine");
            folder.Read("dist/content/resume.en.json").ShouldBe(Samples.English);
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefusesOutputInsideRealAssetsGivenLinkedAssets(bool linkedParent)
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        Sites.WriteAssets(folder);
        folder.Write("wwwroot/dist/keep.txt", "mine");
        var link = Path.Combine(folder.Path, "linked-assets");
        DirectoryLink.Create(link, linkedParent ? folder.Path : Path.Combine(folder.Path, "wwwroot"));
        try
        {
            var assets = linkedParent ? Path.Combine(link, "wwwroot") : link;
            var code = await ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"),
                "--output", Path.Combine(folder.Path, "wwwroot/dist"), "--assets", assets, "--clean"])
                .UseTheme(new FakeTheme()).UseLogging(NullLoggerFactory.Instance).UseEnvironment(_ => null)
                .RunAsync(TestContext.Current.CancellationToken);

            code.ShouldBe(2);
            folder.Read("wwwroot/dist/keep.txt").ShouldBe("mine");
            folder.Exists("wwwroot/dist/index.html").ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Fact]
    public async Task RefusesCredentialsBeforeCleanWithoutLoggingPassword()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        folder.Write("content/resume.en.json", Samples.English.Replace("https://github.com/ann", "https://probe-user:probe-password@example.com/profile", StringComparison.Ordinal));
        folder.Write("dist/keep.txt", "mine");
        var logging = new RecordingLoggerFactory();

        var code = await Generator(folder, null, "--clean").UseLogging(logging).RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(1);
        folder.Read("dist/keep.txt").ShouldBe("mine");
        logging.Lines.ShouldNotContain(line => line.Contains("probe-password", StringComparison.Ordinal));
        logging.Lines.ShouldNotContain(line => line.Contains("probe-user", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RefusesInvalidPreservedSectionBeforeCleaningOutput()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        folder.Write("content/resume.en.json", Samples.English.Replace("\"basics\":", "\"volunteer\": \"not an array\", \"basics\":", StringComparison.Ordinal));
        folder.Write("dist/keep.txt", "mine");

        (await Generator(folder, null, "--clean").RunAsync(TestContext.Current.CancellationToken)).ShouldBe(1);

        folder.Read("dist/keep.txt").ShouldBe("mine");
        folder.Exists("dist/resume.json").ShouldBeFalse();
    }

    [Theory]
    [InlineData("content", "")]
    [InlineData("", "content")]
    [InlineData("", "missing/dist")]
    [InlineData("wwwroot", "")]
    public async Task RefusesCleanGivenOutputThroughLinkedParent(string targetFolder, string suffix)
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        Sites.WriteAssets(folder);
        var link = Path.Combine(folder.Path, "link");
        DirectoryLink.Create(link, Path.Combine(folder.Path, targetFolder));
        try
        {
            var code = await ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"),
                "--output", Path.Combine(link, suffix), "--assets", Path.Combine(folder.Path, "wwwroot"), "--clean"])
                .UseTheme(new FakeTheme())
                .UseLogging(NullLoggerFactory.Instance)
                .RunAsync(TestContext.Current.CancellationToken);

            code.ShouldBe(2);
            folder.Exists("content/resume.en.json").ShouldBeTrue();
            Directory.Exists(Path.Combine(folder.Path, "missing")).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(link);
        }
    }

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

    // --clean empties the output, so it may not be or hold the CV's sources, the program or the working folder.
    [Theory]
    [InlineData("content")]
    [InlineData("")]
    public async Task RefusesOutputGivenFolderThatHoldsContent(string output)
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        var target = Path.Combine(folder.Path, output);

        var code = await ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"), "--output", target, "--assets", Path.Combine(folder.Path, "wwwroot"), "--today", "2026-10-07", "--clean"])
            .UseTheme(new FakeTheme())
            .UseLogging(NullLoggerFactory.Instance)
            .RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(2);
        folder.Exists("content/resume.en.json").ShouldBeTrue();
    }

    // The assets are copied into the output after the pages; an output within them would be copied into itself.
    [Fact]
    public async Task RefusesOutputGivenFolderWithinAssets()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        Sites.WriteAssets(folder);

        var code = await ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"), "--output", Path.Combine(folder.Path, "wwwroot", "dist"), "--assets", Path.Combine(folder.Path, "wwwroot"), "--today", "2026-10-07"])
            .UseTheme(new FakeTheme())
            .UseLogging(NullLoggerFactory.Instance)
            .RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(2);
        Directory.Exists(Path.Combine(folder.Path, "wwwroot", "dist")).ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnsUsageErrorGivenMissingContentFolder()
    {
        using var folder = new TempFolder();

        (await Generator(folder).RunAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
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

    // A writer's own check runs with the validation, before the output is touched.
    [Fact]
    public async Task FailsBeforeWritingGivenWriterRefusingContent()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        folder.Write("dist/keep.txt", "mine");

        (await Generator(folder, null, "--validate-only").UseWriter(new RefusingWriter()).RunAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        (await Generator(folder, null, "--clean").UseWriter(new RefusingWriter()).RunAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        folder.Exists("dist/keep.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task PassesContactEmailToWriterChecksBeforeOutputIsTouched()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        folder.Write("dist/keep.txt", "mine");
        var logging = new RecordingLoggerFactory();

        var code = await Generator(folder, new Dictionary<string, string> { [ResumeGenerator.ContactEmailVariable] = "ann@example.org" }, "--clean")
            .UseWriter(new RefusingWriter())
            .UseLogging(logging)
            .RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(1);
        logging.Lines.ShouldContain(line => line.Contains("/contactEmail", StringComparison.Ordinal));
        folder.Read("dist/keep.txt").ShouldBe("mine");
    }

    [Fact]
    public async Task IgnoresOverriddenWriterCheckGivenActiveReplacement()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);

        (await Generator(folder, null, "--validate-only")
            .UseWriter(new RefusingWriter())
            .UseWriter(new AcceptingWriter())
            .RunAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task FailsBeforeWritingGivenShareImageWriterRefusingContent()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);
        folder.Write("dist/keep.txt", "mine");

        (await Generator(folder, null, "--validate-only").UseShareImages(new RefusingShareImageWriter()).RunAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        folder.Exists("dist/keep.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task FailsGivenRequiredEmailNotSet()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);

        (await Generator(folder, null, "--require-email").RunAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Theory]
    [InlineData("ann.example.com")]
    [InlineData("Ann Example <ann@example.com>")]
    public async Task ReturnsUsageErrorGivenInvalidContactEmail(string email)
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);

        var code = await Generator(folder, new Dictionary<string, string> { [ResumeGenerator.ContactEmailVariable] = email })
            .RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(2);
        Directory.Exists(Path.Combine(folder.Path, "dist")).ShouldBeFalse();
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

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("999999999999999")]
    [InlineData("-62135596801")]
    public async Task ReturnsUsageErrorGivenSourceDateEpochOutOfRange(string epoch)
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);

        var code = await ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"), "--validate-only"])
            .UseLogging(NullLoggerFactory.Instance)
            .UseEnvironment(name => name == ResumeGenerator.SourceDateEpochVariable ? epoch : null)
            .RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(2);
    }

    [Fact]
    public async Task AcceptsMinimumSourceDateEpoch()
    {
        using var folder = new TempFolder();
        Sites.WriteContent(folder);

        var code = await ResumeGenerator.Create(["--content", Path.Combine(folder.Path, "content"), "--validate-only"])
            .UseLogging(NullLoggerFactory.Instance)
            .UseEnvironment(name => name == ResumeGenerator.SourceDateEpochVariable ? "-62135596800" : null)
            .RunAsync(TestContext.Current.CancellationToken);

        code.ShouldBe(0);
    }
}
