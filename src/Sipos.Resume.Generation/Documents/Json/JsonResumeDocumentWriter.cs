using System.Text.Json;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;

namespace Sipos.Resume.Generation.Documents.Json;

/// <summary>Republishes a CV's content file as a JSON Resume download and as the page's <c>resume.json</c>.</summary>
/// <remarks>
/// Decision: the content file as read, written again through the source-generated serializer, with its <c>x-</c>
/// extensions.
/// Why: the content file is the CV's source of truth, already checked to hold no e-mail address; writing it again
/// drops the comments and trailing commas the engine accepts but strict JSON readers do not. Other JSON Resume tools
/// ignore the extensions.
/// Considered: copying the file's bytes, which would publish comments as invalid JSON.
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
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        JsonSerializer.Serialize(writer, source, ResumeJsonContext.Default.JsonResume);
        writer.Flush();
        output.WriteByte((byte)'\n');
    }
}
