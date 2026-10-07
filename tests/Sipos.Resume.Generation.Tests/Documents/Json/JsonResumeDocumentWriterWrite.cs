using System.Text;
using System.Text.Json;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Generation.Documents.Json;
using Sipos.Resume.Generation.Tests.Helpers;

namespace Sipos.Resume.Generation.Tests.Documents.Json;

public class JsonResumeDocumentWriterWrite
{
    // The engine accepts comments and trailing commas; strict JSON readers do not.
    [Fact]
    public void WritesStrictJsonWithExtensionsGivenSourceWithComments()
    {
        var source = Samples.Read(Samples.English.Replace("\"basics\": {", "// The person\n  \"basics\": {", StringComparison.Ordinal));
        using var output = new MemoryStream();

        new JsonResumeDocumentWriter().Write(Contexts.For(SampleDocuments.English(), DownloadFormat.JsonResume, DocumentVariant.Designed, source: source), output);

        var text = Encoding.UTF8.GetString(output.ToArray());
        text.ShouldNotContain("// The person");
        using var json = JsonDocument.Parse(text);
        json.RootElement.GetProperty("basics").GetProperty("name").GetString().ShouldBe("Ann Example");
        json.RootElement.GetProperty("work")[0].GetProperty("x-id").GetString().ShouldBe("acme");
        json.RootElement.GetProperty("$schema").GetString().ShouldStartWith("https://raw.githubusercontent.com/jsonresume/");
    }

    // Other JSON Resume tools import the download; the sections and fields the engine does not show must come along.
    [Fact]
    public void KeepsStandardSectionsTheEngineDoesNotShowGivenPlainJsonResume()
    {
        var json = Samples.English.Replace("\"languages\":", "\"interests\": [ { \"name\": \"Chess\" } ], \"volunteer\": [ { \"organization\": \"Red Cross\" } ], \"languages\":", StringComparison.Ordinal)
            .Replace("\"studyType\": \"BSc\",", "\"studyType\": \"BSc\", \"courses\": [\"Compilers\"],", StringComparison.Ordinal);
        using var output = new MemoryStream();

        new JsonResumeDocumentWriter().Write(Contexts.For(SampleDocuments.English(), DownloadFormat.JsonResume, DocumentVariant.Designed, source: Samples.Read(json)), output);

        using var written = JsonDocument.Parse(output.ToArray());
        written.RootElement.GetProperty("interests")[0].GetProperty("name").GetString().ShouldBe("Chess");
        written.RootElement.GetProperty("volunteer")[0].GetProperty("organization").GetString().ShouldBe("Red Cross");
        written.RootElement.GetProperty("education")[0].GetProperty("courses")[0].GetString().ShouldBe("Compilers");
        Encoding.UTF8.GetString(output.ToArray()).ShouldNotContain('\r');
    }

    [Fact]
    public void KeepsAccentsReadableGivenAccentedNames()
    {
        var source = Samples.Read(Samples.English.Replace("Ann Example", "Sípos Andor", StringComparison.Ordinal));
        using var output = new MemoryStream();

        new JsonResumeDocumentWriter().Write(Contexts.For(SampleDocuments.English(), DownloadFormat.JsonResume, DocumentVariant.Designed, source: source), output);

        Encoding.UTF8.GetString(output.ToArray()).ShouldContain("\"name\": \"Sípos Andor\"");
    }

    [Fact]
    public void ThrowsGivenNoSource() =>
        Should.Throw<InvalidOperationException>(() => new JsonResumeDocumentWriter().Write(Contexts.For(SampleDocuments.English(), DownloadFormat.JsonResume, DocumentVariant.Designed), Stream.Null));
}
