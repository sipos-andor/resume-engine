using System.Text.Json;
using Jint;
using Jint.Native;

namespace Sipos.Resume.Theme.Operandor.Tests.Scripts;

/// <summary>
/// Runs the page's view.js with Jint: what the tailored-view note says and how many link settings the filter button's
/// badge counts.
/// </summary>
public class ViewScript
{
    private const string English = """{"ViewNoteLead":"Tailored view:","ViewNoteShort":"one page","ViewNoteTechnology":"{0} highlighted"}""";

    private const string Data = """{"focus":[{"id":"architect","label":"Software architect"}],"technologies":[{"key":"azure","name":"Azure"}]}""";

    // The search text is not part of a link, so it does not count.
    [Fact]
    public void CountsOnlyLinkSettingsGivenState()
    {
        Count("""{"focus":"architect","technology":"azure","view":"short","query":"x"}""").ShouldBe(3);
        Count("""{"focus":"","technology":"","view":"full","query":"x"}""").ShouldBe(0);
    }

    [Fact]
    public void NamesProfileShortViewAndTechnologyGivenTailoredLink() =>
        Note("""{"focus":"architect","technology":"azure","view":"short"}""", Data, English)
            .ShouldBe("Tailored view: Software architect · one page · Azure highlighted");

    // The owner's example: the lead and the fragments in Hungarian, the technology name kept as it is.
    [Fact]
    public void KeepsSpecWordingGivenHungarianWords() =>
        Note(
            """{"focus":"architect","technology":"dotnet","view":"short"}""",
            """{"focus":[{"id":"architect","label":"Szoftverarchitekt"}],"technologies":[{"key":"dotnet","name":".NET"}]}""",
            """{"ViewNoteLead":"Szabott nézet:","ViewNoteShort":"egy oldal","ViewNoteTechnology":"{0} kiemelve"}""")
            .ShouldBe("Szabott nézet: Szoftverarchitekt · egy oldal · .NET kiemelve");

    [Fact]
    public void NamesTechnologyAloneGivenNoProfile() =>
        Note("""{"focus":"","technology":"azure","view":"full"}""", """{"focus":[],"technologies":[{"key":"azure","name":"Azure"}]}""", English)
            .ShouldBe("Tailored view: Azure highlighted");

    [Fact]
    public void ShowsNothingGivenFullCv() =>
        Note("""{"focus":"","technology":"","view":"full"}""", Data, English).ShouldBe("");

    // The script validates the link's ids against the data; the note still shows nothing for one it does not know.
    [Fact]
    public void ShowsNothingGivenUnknownIds() =>
        Note("""{"focus":"gone","technology":"gone","view":"full"}""", Data, English).ShouldBe("");

    private static readonly Lazy<(Engine Engine, Jint.Native.Object.ObjectInstance Module)> Script = new(() =>
    {
        var engine = new Engine();
        engine.Modules.Add("view", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Scripts", "view.js")));
        return (engine, engine.Modules.Import("view"));
    });

    private static int Count(string state) => (int)Call("activeCount", Parse(state)).AsNumber();

    private static string Note(string state, string data, string words) => Call("noteText", Parse(state), Parse(data), Parse(words)).AsString();

    private static JsValue Parse(string json) => Script.Value.Engine.Evaluate($"JSON.parse({JsonSerializer.Serialize(json)})");

    private static JsValue Call(string function, params JsValue[] arguments) => Script.Value.Engine.Invoke(Script.Value.Module.Get(function), arguments);
}
