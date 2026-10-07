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
    /// <param name="utf8">The file's content.</param>
    public static ReadResult Read(string source, ReadOnlyMemory<byte> utf8)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8, DocumentOptions);
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
            try
            {
                var resume = document.RootElement.Deserialize(ResumeJsonContext.Default.JsonResume);
                return new ReadResult(resume, issues);
            }
            catch (JsonException exception)
            {
                issues.Add(new ValidationIssue(source, exception.Path is { } path ? ToPointer(path) : "", $"Has the wrong shape: {exception.Message}"));
                return new ReadResult(null, issues);
            }
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
