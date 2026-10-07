using Sipos.Resume.Theme.Operandor.Tests.Helpers;

namespace Sipos.Resume.Theme.Operandor.Tests;

public class OperandorThemeFiles
{
    [Fact]
    public void HandsStorageKeysToSharedScriptsGivenSettings()
    {
        var file = new OperandorTheme().Files(ThemePages.Settings).Single();

        file.Path.ShouldBe("/site-config.js");
        file.Content.ShouldContain("themeKey: \"cv-theme\"");
        file.Content.ShouldContain("languageKey: \"cv-language\"");
        file.Content.ShouldNotContain("webAnalyticsToken");
    }

    [Fact]
    public void EscapesTokenGivenAnalytics() =>
        new OperandorTheme().Files(ThemePages.Settings with { AnalyticsToken = "a\"b</script>" }).Single().Content
            .ShouldContain("webAnalyticsToken: \"a\\u0022b\\u003C/script\\u003E\"");

    // The share image's assets and the documents' colours are the design system's.
    [Fact]
    public void UsesDesignSystemColoursGivenDocuments()
    {
        var theme = new OperandorTheme();

        theme.Documents.Accent.ShouldBe("2D7612");
        theme.Documents.SansFamily.ShouldBe("IBM Plex Sans");
        theme.RequiredAssets.ShouldContain("/_content/Operandor.DesignSystem/operandor.css");
    }
}
