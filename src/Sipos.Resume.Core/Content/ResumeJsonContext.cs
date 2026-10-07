using System.Text.Json.Serialization;

namespace Sipos.Resume.Core.Content;

/// <summary>The source-generated serializer of <see cref="JsonResume"/>: no reflection, so it survives trimming.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(JsonResume))]
public sealed partial class ResumeJsonContext : JsonSerializerContext;
