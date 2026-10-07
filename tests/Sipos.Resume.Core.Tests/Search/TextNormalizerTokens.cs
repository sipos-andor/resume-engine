using System.Text.Json;
using Sipos.Resume.Core.Search;

namespace Sipos.Resume.Core.Tests.Search;

/// <summary>Checks the folding rules against the vectors the theme's JavaScript is checked against too.</summary>
public class TextNormalizerTokens
{
    private static readonly JsonElement Vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shared", "text-vectors.json"))).RootElement;

    public static TheoryData<string, string> FoldCases => [.. Vectors.GetProperty("fold").EnumerateArray().Select(c => (c.GetProperty("text").GetString()!, c.GetProperty("folded").GetString()!))];

    public static TheoryData<string, string[]> TokenCases => [.. Vectors.GetProperty("tokens").EnumerateArray().Select(c => (c.GetProperty("text").GetString()!, c.GetProperty("tokens").EnumerateArray().Select(t => t.GetString()!).ToArray()))];

    [Theory]
    [MemberData(nameof(FoldCases))]
    public void FoldsCaseAndDiacriticsGivenText(string text, string folded) => TextNormalizer.Fold(text).ShouldBe(folded);

    [Theory]
    [MemberData(nameof(TokenCases))]
    public void KeepsTechnologyNamesWholeGivenText(string text, string[] tokens) => TextNormalizer.Tokens(text).ShouldBe(tokens);
}
