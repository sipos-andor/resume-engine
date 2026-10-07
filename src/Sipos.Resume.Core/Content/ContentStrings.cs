using System.Text.Json;

namespace Sipos.Resume.Core.Content;

/// <summary>The typed text values of a content file, by JSON pointer, including values that documents do not draw.</summary>
public static class ContentStrings
{
    // Read by the engine, never drawn: the schema link, the language's settings and the job ad aliases.
    private static readonly HashSet<string> NotDrawn = new(StringComparer.Ordinal) { "/$schema", "/meta", "/x-aliases" };

    /// <summary>Returns every text value of a document with its JSON pointer, such as <c>/work/0/highlights/1</c>.</summary>
    /// <param name="resume">The document.</param>
    public static IEnumerable<(string Pointer, string Text)> Of(JsonResume resume)
    {
        ArgumentNullException.ThrowIfNull(resume);
        return Walk(JsonSerializer.SerializeToElement(resume, ResumeJsonContext.Default.JsonResume), "");
    }

    private static IEnumerable<(string Pointer, string Text)> Walk(JsonElement element, string pointer)
    {
        if (NotDrawn.Contains(pointer))
        {
            yield break;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var found in Walk(property.Value, $"{pointer}/{property.Name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal)}"))
                    {
                        yield return found;
                    }
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var found in Walk(item, $"{pointer}/{index++}"))
                    {
                        yield return found;
                    }
                }

                break;
            case JsonValueKind.String:
                yield return (pointer, element.GetString()!);
                break;
            default:
                break;
        }
    }
}
