using System.Text.Json;
using Jint;
using Jint.Native;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Search;

namespace Sipos.Resume.Theme.Operandor.Tests.Scripts;

/// <summary>
/// Runs the page's text.js with Jint against the engine's C#: the same folding and tokens on the shared vectors, the
/// same search hits and the same job ad matches on the sample CV.
/// </summary>
public class TextScriptParity
{
    private static readonly JsonDocument Vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shared", "text-vectors.json")));

    public static TheoryData<string> FoldTexts => [.. Vectors.RootElement.GetProperty("fold").EnumerateArray().Select(item => item.GetProperty("text").GetString()!)];

    public static TheoryData<string> TokenTexts => [.. Vectors.RootElement.GetProperty("tokens").EnumerateArray().Select(item => item.GetProperty("text").GetString()!)];

    [Theory]
    [MemberData(nameof(FoldTexts))]
    public void FoldsAsCSharpGivenVector(string text) => Call("fold", text).AsString().ShouldBe(TextNormalizer.Fold(text));

    [Theory]
    [MemberData(nameof(TokenTexts))]
    public void TokenizesAsCSharpGivenVector(string text) => Strings(Call("tokens", text)).ShouldBe(TextNormalizer.Tokens(text));

    [Theory]
    [InlineData("azure")]
    [InlineData("port glob")]
    [InlineData("C#")]
    [InlineData("hackathon")]
    [InlineData("nothing-like-this")]
    public void FindsAsCSharpGivenQuery(string query)
    {
        var insights = ResumeInsights.Analyze(SampleDocuments.English(), SampleDocuments.Today);
        var index = JsonSerializer.Serialize(new
        {
            entries = insights.Search.Entries.Select(entry => new[] { entry.Id, entry.Title }),
            tokens = insights.Search.Tokens,
        });

        var found = Call("find", Parse(index), query).AsArray().Select(entry => entry.AsArray()[0].AsString());

        found.ShouldBe(insights.Search.Find(query).Select(entry => entry.Id));
    }

    [Theory]
    [InlineData("Senior C# developer with csharp, Azure and Rust")]
    [InlineData("We need LINQ and Blazor.")]
    [InlineData("Java and Go")]
    public void MatchesAsCSharpGivenJobAd(string ad)
    {
        var insights = ResumeInsights.Analyze(SampleDocuments.English(), SampleDocuments.Today);
        var vocabulary = JsonSerializer.Serialize(insights.Vocabulary.Terms.Select(term => new { term = term.Term, phrases = term.Phrases }));

        var matched = Call("match", Parse(vocabulary), ad).AsArray().Select(term => term.AsObject().Get("term").AsString());

        matched.ShouldBe(insights.Vocabulary.Match(ad).Select(term => term.Term));
    }

    private static readonly Lazy<(Engine Engine, Jint.Native.Object.ObjectInstance Module)> Script = new(() =>
    {
        var engine = new Engine();
        engine.Modules.Add("text", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Scripts", "text.js")));
        return (engine, engine.Modules.Import("text"));
    });

    private static JsValue Parse(string json) => Script.Value.Engine.Evaluate($"JSON.parse({JsonSerializer.Serialize(json)})");

    private static JsValue Call(string function, params object[] arguments) =>
        Script.Value.Engine.Invoke(Script.Value.Module.Get(function), arguments.Select(argument => argument is JsValue value ? value : JsValue.FromObject(Script.Value.Engine, argument)).ToArray());

    private static List<string> Strings(JsValue array) => [.. array.AsArray().Select(item => item.AsString())];
}
