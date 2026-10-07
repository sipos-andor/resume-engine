using System.Text.Json.Serialization;

namespace Sipos.Resume.Core.Content;

/// <summary>The source-generated serializer of <see cref="JsonResume"/> and <see cref="SiteFile"/>: no reflection, so it survives trimming.</summary>
/// <remarks>
/// Decision: respect the types' nullable annotations.
/// Why: a list written as <c>null</c>, such as <c>"highlights": null</c>, would otherwise replace the empty default and
/// fail later without a location; now it is a shape issue that names the member.
/// </remarks>
[JsonSourceGenerationOptions(
    RespectNullableAnnotations = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(JsonResume))]
[JsonSerializable(typeof(SiteFile))]
public sealed partial class ResumeJsonContext : JsonSerializerContext;
