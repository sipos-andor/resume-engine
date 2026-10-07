using Sipos.Resume.Generation.Output;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Output;

public class FolderSinkWrite
{
    [Fact]
    public async Task WritesUnderRootGivenSitePath()
    {
        using var folder = new TempFolder();
        var sink = new FolderSink(folder.Path);

        await sink.WriteAsync("/hu/index.html", "x"u8.ToArray(), TestContext.Current.CancellationToken);

        folder.Read("hu/index.html").ShouldBe("x");
        sink.Written.ShouldBe(["/hu/index.html"]);
    }

    // Two outputs for one path mean one silently replaced the other.
    [Fact]
    public async Task ThrowsGivenPathWrittenTwice()
    {
        using var folder = new TempFolder();
        var sink = new FolderSink(folder.Path);
        await sink.WriteAsync("/robots.txt", "a"u8.ToArray(), TestContext.Current.CancellationToken);

        await Should.ThrowAsync<InvalidOperationException>(() => sink.WriteAsync("/Robots.txt", "b"u8.ToArray(), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("relative.txt")]
    [InlineData("/../outside.txt")]
    [InlineData("/folder/")]
    [InlineData("/a//b.txt")]
    [InlineData("/C:/windows.txt")]
    [InlineData("/a/file.txt:stream")]
    public async Task ThrowsGivenPathOutsideSite(string path)
    {
        using var folder = new TempFolder();

        await Should.ThrowAsync<ArgumentException>(() => new FolderSink(folder.Path).WriteAsync(path, "x"u8.ToArray(), TestContext.Current.CancellationToken));
    }
}
