using System.Text.Json;

namespace Sipos.Resume.Core.Validation;

/// <summary>Rejects credentials in HTTP addresses anywhere in content, including preserved fields.</summary>
internal static class ContentUrlGuard
{
    /// <summary>Returns safe diagnostics without quoting the address or its credentials.</summary>
    public static IEnumerable<ValidationIssue> Check(string source, JsonElement element, string pointer = "")
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var name = property.Name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
                    foreach (var issue in Check(source, property.Value, $"{pointer}/{name}"))
                    {
                        yield return issue;
                    }
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var issue in Check(source, item, $"{pointer}/{index++}"))
                    {
                        yield return issue;
                    }
                }

                break;
            case JsonValueKind.String:
                if (Uri.TryCreate(element.GetString(), UriKind.Absolute, out var uri)
                    && uri.Scheme is "http" or "https" && uri.UserInfo.Length > 0)
                {
                    yield return new ValidationIssue(source, pointer, "HTTP addresses must not contain credentials.");
                }

                break;
            default:
                break;
        }
    }
}
