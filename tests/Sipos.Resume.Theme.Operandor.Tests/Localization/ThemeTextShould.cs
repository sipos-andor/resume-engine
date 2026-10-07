using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Sipos.Resume.Theme.Operandor.Localization;

namespace Sipos.Resume.Theme.Operandor.Tests.Localization;

public partial class ThemeTextShould
{
    private static readonly ResourceManager Resources = new("Sipos.Resume.Theme.Operandor.Localization.ThemeText", typeof(ThemeText).Assembly);

    public static TheoryData<string> Satellites => ["hu", "hr", "sr-Latn"];

    // A satellite without a key would silently show English on that language's page.
    [Theory]
    [MemberData(nameof(Satellites))]
    public void HaveTheNeutralKeysWithTheSamePlaceholdersGivenSatellite(string culture)
    {
        var neutral = Entries(CultureInfo.InvariantCulture);
        var satellite = Entries(CultureInfo.GetCultureInfo(culture));

        satellite.Keys.Order().ShouldBe(neutral.Keys.Order());
        foreach (var (key, value) in neutral)
        {
            Placeholders(satellite[key]).ShouldBe(Placeholders(value), key);
            satellite[key].ShouldNotBeNullOrWhiteSpace(key);
        }
    }

    private static Dictionary<string, string> Entries(CultureInfo culture) =>
        Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    private static List<string> Placeholders(string text) => [.. Placeholder().Matches(text).Select(match => match.Value).Order()];

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();
}
