using System.Text.Json;
using Sipos.Resume.Core.Validation;

namespace Sipos.Resume.Core.Tests.Validation;

public class EmailGuardCheck
{
    [Theory]
    [InlineData("name@example.com")]
    [InlineData("Write to first.last+cv@mail.example.co.uk today")]
    [InlineData("mailto:someone")]
    [InlineData("Írjon: józsef.árpád@példa.hu")]
    [InlineData("ann&#64;example.org")]
    [InlineData("ann&commat;example.org")]
    [InlineData("ann%40example.org")]
    [InlineData("ann@example.xn--p1ai")]
    public void FindsAddressGivenText(string text) => EmailGuard.ContainsAddress(text).ShouldBeTrue();

    [Theory]
    [InlineData("C# @ scale")]
    [InlineData("https://operandor.io/hu/kapcsolat?intent=general")]
    [InlineData("@sipos-andor")]
    [InlineData("Mastodon: @ann@mastodon.social")]
    public void FindsNothingGivenTextWithoutAddress(string text) => EmailGuard.ContainsAddress(text).ShouldBeFalse();

    // An issue that quotes a value must not carry an address into the build log.
    [Fact]
    public void ReplacesAddressesGivenTextToLog() =>
        EmailGuard.Redact("Is 'mailto:józsef@példa.hu' or ann@example.org, but en has a mailto: link").ShouldBe("Is '[e-mail address]' or [e-mail address], but en has a mailto: link");

    // The issue points at the value, so the author finds it in a large file.
    [Fact]
    public void PointsToEveryValueWithAddressGivenDocument()
    {
        using var document = JsonDocument.Parse("""{ "basics": { "summary": "a@b.io" }, "work": [ { "highlights": ["ok", "x mailto:y"] } ] }""");

        EmailGuard.Check("resume.en.json", document.RootElement).Select(issue => issue.Path)
            .ShouldBe(["/basics/summary", "/work/0/highlights/1"]);
    }
}
