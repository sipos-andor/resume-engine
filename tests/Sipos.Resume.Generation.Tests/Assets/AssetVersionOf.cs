using Sipos.Resume.Core.Site;
using Sipos.Resume.Generation.Assets;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Assets;

public class AssetVersionOf
{
    // The version changes with a file's content and only then, so a browser fetches the files again exactly when needed.
    [Fact]
    public void ChangesWithContentOnlyGivenSameFiles()
    {
        using var folder = new TempFolder();
        folder.Write("wwwroot/css/app.css", "a{}");
        var root = Path.Combine(folder.Path, "wwwroot");
        ThemeFile[] files = [new("/site-config.js", "config")];

        var first = AssetVersion.Of(root, ["/css/app.css"], files);
        var again = AssetVersion.Of(root, ["/css/app.css"], files);
        folder.Write("wwwroot/css/app.css", "b{}");
        var changed = AssetVersion.Of(root, ["/css/app.css"], files);

        first.ShouldNotBeNull().ShouldMatch("^[0-9a-f]{12}$");
        again.ShouldBe(first);
        changed.ShouldNotBe(first);
    }

    // The files the theme writes, such as the settings script, count as much as the copied ones.
    [Fact]
    public void ChangesGivenOtherThemeFile()
    {
        using var folder = new TempFolder();

        var one = AssetVersion.Of(folder.Path, [], [new("/site-config.js", "a")]);
        var other = AssetVersion.Of(folder.Path, [], [new("/site-config.js", "b")]);

        other.ShouldNotBe(one);
    }

    // An asset the web root lacks is left out, as the copying cannot copy it either.
    [Fact]
    public void LeavesOutMissingAssetGivenWebRootWithoutIt()
    {
        using var folder = new TempFolder();
        folder.Write("wwwroot/css/app.css", "a{}");
        var root = Path.Combine(folder.Path, "wwwroot");

        AssetVersion.Of(root, ["/css/app.css", "/js/missing.js"], []).ShouldBe(AssetVersion.Of(root, ["/css/app.css"], []));
        AssetVersion.Of(root, ["/js/missing.js"], []).ShouldBeNull();
    }
}
