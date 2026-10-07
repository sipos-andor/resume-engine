using System.Collections;
using System.Globalization;
using System.Resources;
using Sipos.Resume.Core.Localization;

namespace Sipos.Resume.Core.Tests.Localization;

public class ResumeLabelsShould
{
    private static readonly ResourceManager Resources = new("Sipos.Resume.Core.Localization.ResumeLabels", typeof(ResumeLabels).Assembly);

    public static TheoryData<string> Satellites => ["hu", "hr", "sr-Latn"];

    // A satellite without a key would silently fall back to English.
    [Theory]
    [MemberData(nameof(Satellites))]
    public void HaveExactlyTheNeutralKeysGivenSatellite(string culture)
    {
        Keys(Resources.GetResourceSet(CultureInfo.GetCultureInfo(culture), createIfNotExists: true, tryParents: false)!)
            .ShouldBe(Keys(Resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)!));
    }

    // Every key the resources hold has its property, and every property reads a key the resources hold.
    [Fact]
    public void ExposeEveryKeyAsProperty()
    {
        var labels = new ResumeLabels(CultureInfo.GetCultureInfo("hu-HU"));
        var properties = typeof(ResumeLabels).GetProperties().Where(p => p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0).ToList();

        properties.Select(p => p.Name).Order().ShouldBe(Keys(Resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)!).Where(k => k != "RatingOf"));
        properties.ShouldAllBe(p => !string.IsNullOrWhiteSpace((string)p.GetValue(labels)!));
    }

    [Theory]
    [InlineData("en", 5, "Expert", "5 of 5")]
    [InlineData("hu-HU", 4, "Haladó", "5-ből 4")]
    [InlineData("hr-HR", 3, "Iskusno", "3 od 5")]
    [InlineData("sr-Latn-RS", 2, "Osnovno", "2 od 5")]
    public void NameLevelsGivenCulture(string culture, int rating, string word, string fraction)
    {
        var labels = new ResumeLabels(CultureInfo.GetCultureInfo(culture));

        labels.Rating(rating).ShouldBe(word);
        labels.RatingOf(rating).ShouldBe(fraction);
    }

    [Theory]
    [InlineData("en-GB", true)]
    [InlineData("hu-HU", true)]
    [InlineData("sr-Latn-RS", true)]
    [InlineData("de-DE", false)]
    [InlineData("sr-Cyrl-RS", false)]
    public void SupportOnlyTheirLanguagesGivenCulture(string culture, bool supported) =>
        ResumeLabels.Supports(CultureInfo.GetCultureInfo(culture)).ShouldBe(supported);

    private static List<string> Keys(ResourceSet set) => [.. set.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).Order(StringComparer.Ordinal)];
}
