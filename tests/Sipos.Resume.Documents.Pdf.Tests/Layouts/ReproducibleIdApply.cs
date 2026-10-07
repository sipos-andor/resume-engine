using System.Text;
using Sipos.Resume.Documents.Pdf.Layouts;

namespace Sipos.Resume.Documents.Pdf.Tests.Layouts;

public class ReproducibleIdApply
{
    private const string Uuid = "1a614a09-913c-370b-b7a5-2e49db7e117f";
    private const string Hex = "1A614A09913C370BB7A52E49DB7E117F";

    private static byte[] Pdf(string uuid, string hex, string body = "1 0 obj") => Encoding.ASCII.GetBytes(
        $"%PDF-1.7\n{body}\n<xmpMM:DocumentID>uuid:{uuid}</xmpMM:DocumentID>\n<xmpMM:InstanceID>uuid:{uuid}</xmpMM:InstanceID>\ntrailer\n<</ID [<{hex}> <{hex}>]>>\n%%EOF");

    [Fact]
    public void ReplacesEveryOccurrenceKeepingLengthGivenSkiaIdentifier()
    {
        var pdf = Pdf(Uuid, Hex);
        var length = pdf.Length;

        ReproducibleId.Apply(pdf).ShouldBeTrue();

        var text = Encoding.ASCII.GetString(pdf);
        pdf.Length.ShouldBe(length);
        text.ShouldNotContain(Uuid);
        text.ShouldNotContain(Hex);
        var replaced = text.Substring(text.IndexOf("uuid:", StringComparison.Ordinal) + 5, 36);
        Guid.TryParseExact(replaced, "D", out _).ShouldBeTrue();
        text.ShouldContain($"<{replaced.Replace("-", "", StringComparison.Ordinal).ToUpperInvariant()}>");
    }

    [Fact]
    public void WritesSameIdentifierGivenSameContentWithAnotherClock()
    {
        var first = Pdf(Uuid, Hex);
        var second = Pdf("7798e412-8829-3ac7-ba2b-e9742046cda3", "7798E41288293AC7BA2BE9742046CDA3");

        ReproducibleId.Apply(first);
        ReproducibleId.Apply(second);

        first.ShouldBe(second);
    }

    [Fact]
    public void WritesAnotherIdentifierGivenOtherContent()
    {
        var first = Pdf(Uuid, Hex);
        var second = Pdf(Uuid, Hex, "2 0 obj");

        ReproducibleId.Apply(first);
        ReproducibleId.Apply(second);

        Encoding.ASCII.GetString(first).Split("uuid:")[1][..36].ShouldNotBe(Encoding.ASCII.GetString(second).Split("uuid:")[1][..36]);
    }

    [Fact]
    public void LeavesFileAsWrittenGivenNoXmpIdentifier()
    {
        var pdf = Encoding.ASCII.GetBytes($"%PDF-1.7\ntrailer\n<</ID [<{Hex}> <{Hex}>]>>\n%%EOF");
        var original = pdf.ToArray();

        ReproducibleId.Apply(pdf).ShouldBeFalse();

        pdf.ShouldBe(original);
    }
}
