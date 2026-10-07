using System.Text.Json;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Content;

/// <summary>The outcome of reading a content file: the document when it could be read, and every issue found.</summary>
/// <param name="Resume">The document, or <see langword="null"/> when the file is not valid JSON of the right shape.</param>
/// <param name="Issues">The problems found while reading, such as a syntax error or an e-mail address.</param>
public sealed record ReadResult(JsonResume? Resume, IReadOnlyList<ValidationIssue> Issues);

/// <summary>Reads a JSON Resume file into a <see cref="JsonResume"/>.</summary>
public static class ResumeReader
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Reads a file's UTF-8 bytes, checks them for e-mail addresses and maps them onto the document type.</summary>
    /// <param name="source">The file's name, for the issues, such as <c>resume.hu.json</c>.</param>
    /// <param name="utf8">The file's content, with or without a byte order mark.</param>
    public static ReadResult Read(string source, ReadOnlyMemory<byte> utf8)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(WithoutByteOrderMark(utf8), DocumentOptions);
        }
        catch (JsonException exception)
        {
            return new ReadResult(null, [new ValidationIssue(source, "", $"Not valid JSON: {exception.Message}")]);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new ReadResult(null, [new ValidationIssue(source, "", "The file must hold a JSON object.")]);
            }

            var issues = EmailGuard.Check(source, document.RootElement).ToList();
            var credentials = ContentUrlGuard.Check(source, document.RootElement).ToList();
            if (credentials.Count > 0)
            {
                // Do not let parity diagnostics quote a rejected address from this document.
                return new ReadResult(null, [.. issues, .. credentials]);
            }

            var nulls = NullItems(document.RootElement, "").Select(pointer => new ValidationIssue(source, pointer, "Must not be null; leave the entry out instead.")).ToList();
            if (nulls.Count > 0)
            {
                return new ReadResult(null, [.. issues, .. nulls]);
            }

            try
            {
                var resume = document.RootElement.Deserialize(ResumeJsonContext.Default.JsonResume)! with { Original = document.RootElement.Clone() };
                return new ReadResult(resume, issues);
            }
            catch (JsonException exception)
            {
                issues.Add(new ValidationIssue(source, exception.Path is { } path ? ToPointer(path) : "", $"Has the wrong shape: {exception.Message}"));
                return new ReadResult(null, issues);
            }
        }
    }

    /// <summary>Returns UTF-8 content without the byte order mark some Windows editors write before it.</summary>
    /// <param name="utf8">The content.</param>
    /// <remarks>
    /// RFC 8259 lets a parser ignore the mark, but only System.Text.Json's stream readers do; the span and memory
    /// readers report it as an invalid value.
    /// </remarks>
    public static ReadOnlyMemory<byte> WithoutByteOrderMark(ReadOnlyMemory<byte> utf8) =>
        utf8.Span.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? utf8[3..] : utf8;

    // A null entry of a list, such as "work": [null], would reach the validator as an item without members.
    private static IEnumerable<string> NullItems(JsonElement element, string pointer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var found in NullItems(property.Value, $"{pointer}/{property.Name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal)}"))
                    {
                        yield return found;
                    }
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Null)
                    {
                        yield return $"{pointer}/{index}";
                    }
                    else
                    {
                        foreach (var found in NullItems(item, $"{pointer}/{index}"))
                        {
                            yield return found;
                        }
                    }

                    index++;
                }

                break;
            default:
                break;
        }
    }

    // System.Text.Json reports paths like $.work[2].startDate; issues use JSON pointers like /work/2/startDate.
    private static string ToPointer(string path) =>
        path.TrimStart('$').Replace("[", "/", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal).Replace('.', '/') switch
        {
            "" => "",
            var pointer => pointer.StartsWith('/') ? pointer : "/" + pointer,
        };
}
