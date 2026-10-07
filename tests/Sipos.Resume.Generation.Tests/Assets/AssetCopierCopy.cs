using Sipos.Resume.Generation.Assets;
using Sipos.Resume.Generation.Output;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Assets;

public class AssetCopierCopy
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopiesRegularAssetsWithoutFollowingDirectoryLinks(bool cycle)
    {
        using var folder = new TempFolder();
        folder.Write("wwwroot/nested/app.css", "public");
        folder.Write("wwwroot/.hidden", "hidden asset");
        folder.Write("wwwroot/nested/app.css.gz", "compressed");
        folder.Write("outside/secret.txt", "private");
        var assets = Path.Combine(folder.Path, "wwwroot");
        var link = Path.Combine(assets, "nested", "linked");
        DirectoryLink.Create(link, cycle ? assets : Path.Combine(folder.Path, "outside"));
        try
        {
            var sink = new FolderSink(Path.Combine(folder.Path, "dist"));

            var count = await AssetCopier.CopyAsync(assets, sink, TestContext.Current.CancellationToken);

            count.ShouldBe(2);
            sink.Written.ShouldBe(["/.hidden", "/nested/app.css"]);
            folder.Read("dist/nested/app.css").ShouldBe("public");
            folder.Read("dist/.hidden").ShouldBe("hidden asset");
            folder.Exists("dist/nested/linked/secret.txt").ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Fact]
    public async Task SkipsFileLinks()
    {
        using var folder = new TempFolder();
        var target = folder.Write("outside/secret.txt", "private");
        folder.Write("wwwroot/app.css", "public");
        var assets = Path.Combine(folder.Path, "wwwroot");
        var link = Path.Combine(assets, "secret.txt");
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows requires Developer Mode or symbolic-link privileges for file links.");
        }

        try
        {
            var sink = new FolderSink(Path.Combine(folder.Path, "dist"));

            (await AssetCopier.CopyAsync(assets, sink, TestContext.Current.CancellationToken)).ShouldBe(1);
            sink.Written.ShouldBe(["/app.css"]);
            folder.Exists("dist/secret.txt").ShouldBeFalse();
        }
        finally
        {
            File.Delete(link);
        }
    }
}
