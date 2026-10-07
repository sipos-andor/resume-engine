using NJsonSchema;

namespace Sipos.Resume.Core.Tests.Content;

/// <summary>
/// The engine's content files stay JSON Resume: the official v1.2.1 schema (tests/Shared/jsonresume-schema-v1.2.1.json,
/// MIT, from jsonresume/resume-schema, verbatim but for the example address of the e-mail field) accepts them with
/// their x- extensions, so any JSON Resume tool can read them. v1.2.1 leaves the root open for extensions.
/// </summary>
public class JsonResumeSchemaValidate
{
    private static readonly Lazy<JsonSchema> Schema = new(() =>
        JsonSchema.FromJsonAsync(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shared", "jsonresume-schema-v1.2.1.json"))).GetAwaiter().GetResult());

    [Fact]
    public void AcceptsSampleWithExtensionsGivenOfficialSchema() => Schema.Value.Validate(Samples.English).ShouldBeEmpty();

    [Fact]
    public void AcceptsPlainSampleGivenOfficialSchema() =>
        Schema.Value.Validate(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shared", "plain-resume.en.json"))).ShouldBeEmpty();

    // The schema is the arbiter here, not the engine's own reader: a wrong date shape is a schema error too.
    [Fact]
    public void RejectsDateOutsideIso8601GivenOfficialSchema() =>
        Schema.Value.Validate(Samples.English.Replace("\"2022-11\"", "\"Nov 2022\"", StringComparison.Ordinal)).ShouldNotBeEmpty();
}
