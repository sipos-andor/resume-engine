using System.Text.Json;
using Microsoft.Extensions.Localization;
using Sipos.Resume.Theme.Operandor.Localization;
using Sipos.Resume.Theme.Operandor.Rendering;
using Sipos.Resume.Theme.Operandor.Tests.Helpers;

namespace Sipos.Resume.Theme.Operandor.Tests.Rendering;

public class ResumeDataJson
{
    private static readonly IStringLocalizer<ThemeText> Text = new BracketWords();

    [Fact]
    public void CarriesWhatScriptAppliesGivenPage()
    {
        var page = ThemePages.Sample()[0];

        var json = JsonDocument.Parse(ResumeData.Json(page, Text, new Dictionary<string, string> { ["SwitchToDark"] = "Dark" })).RootElement;

        json.GetProperty("search").GetProperty("entries")[0][0].GetString().ShouldBe(page.Insights.Search.Entries[0].Id);
        json.GetProperty("technologies").EnumerateArray().Select(entry => entry.GetProperty("key").GetString()).ShouldBe(page.Insights.Technologies.Technologies.Select(entry => entry.Key));
        json.GetProperty("focus")[0].GetProperty("id").GetString().ShouldBe("architect");
        json.GetProperty("vocabulary").GetArrayLength().ShouldBe(page.Insights.Vocabulary.Terms.Count);
        json.GetProperty("text").GetProperty("SwitchToDark").GetString().ShouldBe("Dark");
        json.GetProperty("text").GetProperty("LinkCopied").GetString().ShouldBe("[LinkCopied]");
    }

    // No text of the CV can close the data element, and accented words stay readable.
    [Fact]
    public void EscapesMarkupAndKeepsLettersGivenScriptInContent()
    {
        var page = ThemePages.Sample()[0];
        var document = page.Document with { Strengths = [page.Document.Strengths[0] with { Title = "</script><b>Sípos</b>" }] };
        page = page with { Document = document, Insights = Sipos.Resume.Core.Insights.ResumeInsights.Analyze(document, SampleDocuments.Today) };

        var json = ResumeData.Json(page, Text, new Dictionary<string, string>());

        json.ShouldNotContain("</script>");
        json.ShouldNotContain("<b>");
        json.ShouldContain("Sípos");
    }

    // Returns every key in brackets, so a test sees which word the data carries.
    private sealed class BracketWords : IStringLocalizer<ThemeText>
    {
        public LocalizedString this[string name] => new(name, $"[{name}]");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
