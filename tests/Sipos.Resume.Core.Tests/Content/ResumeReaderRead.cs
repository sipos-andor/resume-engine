using System.Text;
using Sipos.Resume.Core.Content;

namespace Sipos.Resume.Core.Tests.Content;

public class ResumeReaderRead
{
    [Theory]
    [InlineData("{\"basics\":{\"url\":\"https://probe-user:probe-password@example.com/\"}}", "/basics/url")]
    [InlineData("{\"volunteer\":[{\"url\":\"http://probe-user:probe-password@example.com/\"}]}", "/volunteer/0/url")]
    [InlineData("{\"publications\":[{\"url\":\"https://probe-user@example.com/\"}]}", "/publications/0/url")]
    [InlineData("{\"meta\":{\"canonical\":\"https://probe-user:probe-password@example.com/\"}}", "/meta/canonical")]
    [InlineData("{\"x-extra\":[{\"a~/b\":\"https://probe-user:probe-password@example.com/\"}]}", "/x-extra/0/a~0~1b")]
    public void RejectsCredentialsAnywhereWithoutQuotingThem(string json, string path)
    {
        var result = ResumeReader.Read("resume.en.json", Encoding.UTF8.GetBytes(json));

        result.Resume.ShouldBeNull();
        result.Issues.ShouldContain(issue => issue.Path == path && issue.Message.Contains("credentials", StringComparison.Ordinal));
        result.Issues.ShouldAllBe(issue => !issue.Message.Contains("probe-password", StringComparison.Ordinal)
            && !issue.Message.Contains("probe-user", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadsStandardAndExtensionFieldsGivenValidFile()
    {
        var result = ResumeReader.Read("resume.en.json", Encoding.UTF8.GetBytes(Samples.English));

        result.Issues.ShouldBeEmpty();
        var resume = result.Resume!;
        resume.Basics!.Name.ShouldBe("Ann Example");
        resume.Basics.Contact!.Url.ShouldBe("https://example.com/contact");
        resume.Work[0].Id.ShouldBe("acme");
        resume.Work[0].Short.ShouldBeTrue();
        resume.Projects[0].Work.ShouldBe("acme");
        resume.Skills[0].Rating.ShouldBe(5);
        resume.FocusProfiles.Single().Id.ShouldBe("architect");
        resume.Aliases["C#"].ShouldBe(["csharp"]);
        resume.Meta!.OgLocale.ShouldBe("en_GB");
    }

    [Fact]
    public void ReportsSyntaxErrorGivenBrokenJson()
    {
        var result = ResumeReader.Read("resume.en.json", "{ \"basics\": "u8.ToArray());

        result.Resume.ShouldBeNull();
        result.Issues.Single().Message.ShouldStartWith("Not valid JSON");
    }

    [Fact]
    public void ReportsShapeErrorGivenWrongType()
    {
        var result = ResumeReader.Read("resume.en.json", """{ "work": { "name": "x" } }"""u8.ToArray());

        result.Resume.ShouldBeNull();
        result.Issues.Single().Path.ShouldBe("/work");
    }

    // Windows editors often save UTF-8 with a byte order mark, which RFC 8259 lets a reader ignore.
    [Fact]
    public void ReadsFileGivenByteOrderMark()
    {
        var result = ResumeReader.Read("resume.en.json", (byte[])[.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Samples.English)]);

        result.Issues.ShouldBeEmpty();
        result.Resume!.Basics!.Name.ShouldBe("Ann Example");
    }

    // An explicit null would replace a list's empty default and fail later without a location.
    [Theory]
    [InlineData("""{ "work": [ { "name": "x", "highlights": null } ] }""", "/work/0/highlights")]
    [InlineData("""{ "work": [ null ] }""", "/work/0")]
    [InlineData("""{ "skills": [ { "keywords": ["C#", null] } ] }""", "/skills/0/keywords/1")]
    public void ReportsNullGivenNullListOrEntry(string json, string path)
    {
        var result = ResumeReader.Read("resume.en.json", Encoding.UTF8.GetBytes(json));

        result.Resume.ShouldBeNull();
        result.Issues.Single().Path.ShouldBe(path);
    }

    // A file may not even hold an address, whatever the field.
    [Fact]
    public void ReportsAddressGivenAddressInSummary()
    {
        var result = ResumeReader.Read("resume.en.json", Encoding.UTF8.GetBytes(Samples.English.Replace("Builds .NET systems.", "Mail ann@example.com", StringComparison.Ordinal)));

        result.Issues.Single().Path.ShouldBe("/basics/summary");
    }
}
