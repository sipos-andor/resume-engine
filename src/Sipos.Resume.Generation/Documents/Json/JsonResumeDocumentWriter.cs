using System.Text.Json;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;

namespace Sipos.Resume.Generation.Documents.Json;

/// <summary>Republishes a CV's content file as a JSON Resume download and as the page's <c>resume.json</c>.</summary>
/// <remarks>
/// Decision: the content file as parsed, every member written again, the <c>x-</c> extensions and the standard
/// sections the engine does not show (<c>volunteer</c>, <c>interests</c>, <c>references</c>, …) included.
/// Why: the content file is the CV's source of truth, already checked to hold no e-mail address in any member; writing
/// the parsed document again drops the comments and trailing commas the engine accepts but strict JSON readers do not,
/// and keeps what other JSON Resume tools import. Other tools ignore the extensions.
/// Considered: copying the file's bytes, which would publish comments as invalid JSON; serializing the engine's typed
/// model, which leaves out every member it does not know.
/// </remarks>
public sealed class JsonResumeDocumentWriter : IDocumentWriter
{
    /// <inheritdoc />
    public DownloadFormat Format => DownloadFormat.JsonResume;

    /// <inheritdoc />
    public bool Supports(DocumentVariant variant) => variant == DocumentVariant.Designed;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The context carries no content file.</exception>
    public void Write(DocumentContext context, Stream output)
    {
        ArgumentNullException.ThrowIfNull(context);
        var source = context.Source ?? throw new InvalidOperationException("A JSON Resume download needs the content file it republishes.");
        // A line feed on every system, so a build on Windows gives the same bytes.
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true, NewLine = "\n", Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        if (source.Original is { } original)
        {
            original.WriteTo(writer);
        }
        else
        {
            JsonSerializer.Serialize(writer, source, ResumeJsonContext.Default.JsonResume);
        }

        writer.Flush();
        output.WriteByte((byte)'\n');
    }
}
