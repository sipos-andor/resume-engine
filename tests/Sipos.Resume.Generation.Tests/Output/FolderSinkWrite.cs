using Sipos.Resume.Generation.Output;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Output;

public class FolderSinkWrite
{
    [Theory]
    [InlineData("/keep.txt")]
    [InlineData("/new/keep.txt")]
    public async Task RejectsLinkedParentBeforeWriting(string path)
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "target");
        Directory.CreateDirectory(target);
        folder.Write("target/keep.txt", "mine");
        var link = Path.Combine(folder.Path, "link");
        DirectoryLink.Create(link, target);
        try
        {
            var sink = new FolderSink(link);
            await Should.ThrowAsync<IOException>(() => sink.WriteAsync(path, "x"u8.ToArray(), TestContext.Current.CancellationToken));

            sink.Written.ShouldBeEmpty();
            folder.Read("target/keep.txt").ShouldBe("mine");
            Directory.Exists(Path.Combine(target, "new")).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Fact]
    public async Task RejectsLinkedChildBeforeWriting()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "target");
        Directory.CreateDirectory(target);
        var root = Path.Combine(folder.Path, "dist");
        Directory.CreateDirectory(root);
        var link = Path.Combine(root, "downloads");
        DirectoryLink.Create(link, target);
        try
        {
            await Should.ThrowAsync<IOException>(() => new FolderSink(root)
                .WriteAsync("/downloads/cv.txt", "x"u8.ToArray(), TestContext.Current.CancellationToken));
            folder.Exists("target/cv.txt").ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(link);
        }
    }

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
    [InlineData("/assets/app?.js")]
    [InlineData("/assets/app#.js")]
    [InlineData("/assets/app%.js")]
    [InlineData("/assets/app*.js")]
    [InlineData("/assets/app<.js")]
    [InlineData("/assets/app>.js")]
    [InlineData("/assets/app|.js")]
    [InlineData("/assets/app\".js")]
    [InlineData("/assets/trailing.")]
    [InlineData("/assets/trailing ")]
    [InlineData("/assets/CON")]
    [InlineData("/assets/con.txt")]
    [InlineData("/assets/LPT9.log")]
    public async Task ThrowsGivenPathOutsideSite(string path)
    {
        using var folder = new TempFolder();

        await Should.ThrowAsync<ArgumentException>(() => new FolderSink(folder.Path).WriteAsync(path, "x"u8.ToArray(), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("/.nojekyll")]
    [InlineData("/assets/app.min.js")]
    [InlineData("/hu-HU/index.html")]
    public async Task WritesPortableFilenameGivenSitePath(string path)
    {
        using var folder = new TempFolder();

        await new FolderSink(folder.Path).WriteAsync(path, "x"u8.ToArray(), TestContext.Current.CancellationToken);

        folder.Read(path[1..]).ShouldBe("x");
    }
}
