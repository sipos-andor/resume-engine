using System.Text;
using Sipos.Resume.Documents.Pdf.Layouts;

namespace Sipos.Resume.Documents.Pdf.Tests.Layouts;

public class ReproducibleIdApply
{
    private const string Uuid = "1a614a09-913c-370b-b7a5-2e49db7e117f";
    private const string Hex = "<1A614A09913C370BB7A52E49DB7E117F>";

    // Skia writes each trailer string as hex or, when shorter, as a literal with escapes, such as this one with an
    // escaped parenthesis and an octal byte.
    private const string Literal = @"(\024[v\237iB5s\265;zV\)H\317\033#)";

    // The shape Skia writes: the XMP with the identifier twice, the cross-reference table, then the trailer.
    private static byte[] Pdf(string uuid, string id, string body = "1 0 obj")
    {
        var head = $"%PDF-1.7\n{body}\n<xmpMM:DocumentID>uuid:{uuid}</xmpMM:DocumentID>\n<xmpMM:InstanceID>uuid:{uuid}</xmpMM:InstanceID>\n";
        return Encoding.Latin1.GetBytes($"{head}xref\n0 1\ntrailer\n<</Size 1\n/ID [{id} {id}]>>\nstartxref\n{head.Length}\n%%EOF");
    }

    private static string Text(byte[] pdf) => Encoding.Latin1.GetString(pdf);

    [Theory]
    [InlineData(Hex)]
    [InlineData(Literal)]
    public void ReplacesIdentifierWithContentHashGivenSkiaIdentifier(string id)
    {
        var text = Text(ReproducibleId.Apply(Pdf(Uuid, id)).ShouldNotBeNull());

        text.ShouldNotContain(Uuid);
        text.ShouldNotContain(id);
        var replaced = text.Substring(text.IndexOf("uuid:", StringComparison.Ordinal) + 5, 36);
        Guid.TryParseExact(replaced, "D", out _).ShouldBeTrue();
        var hex = replaced.Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
        text.ShouldContain($"/ID [<{hex}> <{hex}>]>>\nstartxref");
    }

    // The clock decides both the identifier and whether Skia writes it as hex or as a literal.
    [Theory]
    [InlineData(Hex)]
    [InlineData(Literal)]
    public void WritesSameBytesGivenSameContentWithAnotherClock(string id) =>
        ReproducibleId.Apply(Pdf(Uuid, id)).ShouldBe(ReproducibleId.Apply(Pdf("7798e412-8829-3ac7-ba2b-e9742046cda3", "<7798E41288293AC7BA2BE9742046CDA3>")));

    [Fact]
    public void WritesAnotherIdentifierGivenOtherContent() =>
        Text(ReproducibleId.Apply(Pdf(Uuid, Hex))!).Split("uuid:")[1][..36].ShouldNotBe(Text(ReproducibleId.Apply(Pdf(Uuid, Hex, "2 0 obj"))!).Split("uuid:")[1][..36]);

    [Fact]
    public void ReturnsNothingGivenNoXmpIdentifier() =>
        ReproducibleId.Apply(Encoding.ASCII.GetBytes($"%PDF-1.7\nxref\ntrailer\n<</ID [{Hex} {Hex}]>>\nstartxref\n9\n%%EOF")).ShouldBeNull();

    [Fact]
    public void ReturnsNothingGivenUnbalancedLiteral() =>
        ReproducibleId.Apply(Pdf(Uuid, @"(\024(v")).ShouldBeNull();
}
