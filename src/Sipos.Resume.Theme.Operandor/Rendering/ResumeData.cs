using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Localization;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Theme.Operandor.Localization;

namespace Sipos.Resume.Theme.Operandor.Rendering;

/// <summary>
/// The data block the page's script reads (<c>&lt;script type="application/json" id="cv-data"&gt;</c>): the search
/// index, the technologies, the views of the position profiles, the job ad vocabulary and the script's words.
/// </summary>
/// <remarks>
/// <para>
/// Decision: everything is derived in C# at build time and handed to the script as data; the script only applies it.
/// Why: the engine's tests cover one implementation of each rule; the script stays small, and its text folding and
/// matching are checked against the same vectors as the C# (Jint tests).
/// </para>
/// <para>
/// Decision: written with <see cref="Utf8JsonWriter"/> and an encoder that allows every Unicode letter.
/// Why: the encoder still escapes <c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c> and quotes, so no text of the CV can close the
/// element, while accented words stay as short as they are.
/// </para>
/// </remarks>
internal static class ResumeData
{
    private static readonly JavaScriptEncoder Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

    /// <summary>The element id of the data block.</summary>
    public const string ElementId = "cv-data";

    /// <summary>Returns the data block's JSON.</summary>
    /// <param name="page">The page.</param>
    /// <param name="text">The theme's words in the page's language.</param>
    /// <param name="theme">The theme toggle's words: the labels for switching to dark, light and system.</param>
    public static string Json(SitePage page, IStringLocalizer<ThemeText> text, IReadOnlyDictionary<string, string> theme)
    {
        var insights = page.Insights;
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = Encoder }))
        {
            json.WriteStartObject();
            json.WriteStartObject("search");
            json.WriteStartArray("entries");
            foreach (var entry in insights.Search.Entries)
            {
                json.WriteStartArray();
                json.WriteStringValue(entry.Id);
                json.WriteStringValue(entry.Title);
                json.WriteEndArray();
            }

            json.WriteEndArray();
            json.WriteStartObject("tokens");
            foreach (var (token, indexes) in insights.Search.Tokens.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                json.WriteStartArray(token);
                foreach (var index in indexes)
                {
                    json.WriteNumberValue(index);
                }

                json.WriteEndArray();
            }

            json.WriteEndObject();
            json.WriteEndObject();

            json.WriteStartArray("technologies");
            foreach (var technology in insights.Technologies.Technologies)
            {
                json.WriteStartObject();
                json.WriteString("name", technology.Name);
                json.WriteString("key", technology.Key);
                WriteStrings(json, "items", technology.ItemIds);
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartArray("focus");
            foreach (var view in insights.Focus)
            {
                json.WriteStartObject();
                json.WriteString("id", view.Profile.Id);
                json.WriteString("label", view.Profile.Label);
                if (view.Profile.Summary is { } summary)
                {
                    json.WriteString("summary", summary);
                }

                WriteStrings(json, "strengths", view.StrengthOrder);
                json.WriteStartArray("skillGroups");
                foreach (var index in view.SkillGroupOrder)
                {
                    json.WriteNumberValue(index);
                }

                json.WriteEndArray();
                WriteStrings(json, "emphasized", [.. view.Emphasized.Order(StringComparer.Ordinal)]);
                WriteStrings(json, "skills", [.. view.EmphasizedSkills.Order(StringComparer.Ordinal)]);
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartArray("vocabulary");
            foreach (var term in insights.Vocabulary.Terms)
            {
                json.WriteStartObject();
                json.WriteString("term", term.Term);
                json.WriteStartArray("phrases");
                foreach (var phrase in term.Phrases)
                {
                    json.WriteStartArray();
                    foreach (var token in phrase)
                    {
                        json.WriteStringValue(token);
                    }

                    json.WriteEndArray();
                }

                json.WriteEndArray();
                WriteStrings(json, "items", term.ItemIds);
                json.WriteBoolean("skill", term.IsSkill);
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartObject("text");
            foreach (var key in ScriptKeys)
            {
                json.WriteString(key, text[key]);
            }

            foreach (var (key, value) in theme)
            {
                json.WriteString(key, value);
            }

            json.WriteEndObject();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>The theme's words the script shows: result counts, the copy confirmation, the matcher's results.</summary>
    public static IReadOnlyList<string> ScriptKeys { get; } =
        ["SearchResults", "SearchNoResults", "TechnologyItems", "LinkCopied", "MatchFound", "MatchNone", "MatchCoverage"];

    private static void WriteStrings(Utf8JsonWriter json, string name, IEnumerable<string> values)
    {
        json.WriteStartArray(name);
        foreach (var value in values)
        {
            json.WriteStringValue(value);
        }

        json.WriteEndArray();
    }
}
