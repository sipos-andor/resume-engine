using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Documents.Pdf.Fonts;

namespace Sipos.Resume.Documents.Pdf.Tests.Fonts;

public class PlexFontsResolve
{
    [Fact]
    public void KeepsFamiliesGivenRegisteredPlex() =>
        PlexFonts.Resolve(DocumentTheme.Neutral).ShouldBe(DocumentTheme.Neutral);

    [Fact]
    public void FallsBackToPlexGivenUnknownFamilies()
    {
        var theme = PlexFonts.Resolve(DocumentTheme.Neutral with { SansFamily = "Inter", MonoFamily = "JetBrains Mono" });

        theme.SansFamily.ShouldBe(PlexFonts.Sans);
        theme.MonoFamily.ShouldBe(PlexFonts.Mono);
    }
}
