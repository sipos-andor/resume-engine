using System.Text.Json;
using NJsonSchema;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Tests.Validation;

public class PreservedResumeValidatorCheck
{
    private static readonly Lazy<JsonSchema> Schema = new(() =>
        JsonSchema.FromJsonAsync(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shared", "jsonresume-schema-v1.2.1.json"))).GetAwaiter().GetResult());

    [Theory]
    [InlineData("{\"volunteer\":\"not an array\"}", "/volunteer")]
    [InlineData("{\"publications\":{}}", "/publications")]
    [InlineData("{\"interests\":null}", "/interests")]
    [InlineData("{\"references\":[42]}", "/references/0")]
    [InlineData("{\"volunteer\":[{\"organization\":42}]}", "/volunteer/0/organization")]
    [InlineData("{\"volunteer\":[{\"highlights\":\"text\"}]}", "/volunteer/0/highlights")]
    [InlineData("{\"volunteer\":[{\"highlights\":[true]}]}", "/volunteer/0/highlights/0")]
    [InlineData("{\"volunteer\":[{\"startDate\":\"October 2026\"}]}", "/volunteer/0/startDate")]
    [InlineData("{\"volunteer\":[{\"endDate\":7}]}", "/volunteer/0/endDate")]
    [InlineData("{\"volunteer\":[{\"url\":\"relative/path\"}]}", "/volunteer/0/url")]
    [InlineData("{\"publications\":[{\"releaseDate\":\"Oct 2026\"}]}", "/publications/0/releaseDate")]
    [InlineData("{\"publications\":[{\"publisher\":null}]}", "/publications/0/publisher")]
    [InlineData("{\"publications\":[{\"summary\":false}]}", "/publications/0/summary")]
    [InlineData("{\"publications\":[{\"url\":7}]}", "/publications/0/url")]
    [InlineData("{\"interests\":[{\"keywords\":[{}]}]}", "/interests/0/keywords/0")]
    [InlineData("{\"references\":[{\"reference\":[]}]}", "/references/0/reference")]
    [InlineData("{\"education\":[{\"score\":3.5}]}", "/education/0/score")]
    [InlineData("{\"education\":[{\"courses\":null}]}", "/education/0/courses")]
    [InlineData("{\"education\":[{\"courses\":[42]}]}", "/education/0/courses/0")]
    public void RejectsSchemaInvalidPreservedFields(string json, string pointer)
    {
        Schema.Value.Validate(json).ShouldNotBeEmpty();
        using var parsed = JsonDocument.Parse(json);

        var issue = PreservedResumeValidator.Check("resume.en.json", parsed.RootElement).ShouldHaveSingleItem();

        issue.Path.ShouldBe(pointer);
        issue.Source.ShouldBe("resume.en.json");
    }

    [Fact]
    public void AcceptsValidPreservedSectionsAndUnknownExtensions()
    {
        const string json = """
            {
              "volunteer": [{ "organization": "Red Cross", "position": "Volunteer", "url": "https://example.com/",
                "startDate": "2020", "endDate": "2021-03-17", "summary": "Helped", "highlights": ["Organized"], "x-extra": {"count": 1} }],
              "publications": [{ "name": "Paper", "publisher": "IEEE", "releaseDate": "2022-06", "url": "https://example.com/paper", "summary": "Research" }],
              "interests": [{"name": "Chess", "keywords": ["Tournaments"]}],
              "references": [{"name": "Jane", "reference": "Recommended"}],
              "education": [{"score": "3.5", "courses": ["Compilers"]}],
              "x-unknown": {"custom": 42}
            }
            """;
        Schema.Value.Validate(json).ShouldBeEmpty();
        using var parsed = JsonDocument.Parse(json);

        PreservedResumeValidator.Check("resume.en.json", parsed.RootElement).ShouldBeEmpty();
    }
}
