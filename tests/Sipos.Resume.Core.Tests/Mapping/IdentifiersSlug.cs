using Sipos.Resume.Core.Mapping;

namespace Sipos.Resume.Core.Tests.Mapping;

public class IdentifiersSlug
{
    [Theory]
    [InlineData("ALLWIN Informatika Kft. 2015-03", "allwin-informatika-kft-2015-03")]
    [InlineData("Szabadkai Műszaki Főiskola", "szabadkai-muszaki-foiskola")]
    [InlineData("Čačak – Đurđevo", "cacak-durdevo")]
    [InlineData("  .NET & C#  ", "net-c")]
    public void FoldsToAsciiHyphenatedGivenText(string text, string slug)
    {
        Identifiers.Slug(text).ShouldBe(slug);
        Identifiers.IsValid(slug).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Acme")]
    [InlineData("-acme")]
    [InlineData("ac me")]
    [InlineData("")]
    public void RefusesGivenNotLowercaseHyphenated(string id) => Identifiers.IsValid(id).ShouldBeFalse();
}
