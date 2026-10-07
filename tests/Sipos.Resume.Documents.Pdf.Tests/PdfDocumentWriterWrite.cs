using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Model;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Tokens;

namespace Sipos.Resume.Documents.Pdf.Tests;

public class PdfDocumentWriterWrite
{
    [Fact]
    public void WritesNameTitleSectionsPeriodAndHighlightGivenDesignedLayout()
    {
        var context = PdfSamples.Context(SampleDocuments.English(), DocumentVariant.Designed);
        var outline = DocumentOutline.Of(context);

        var text = PdfSamples.Text(PdfSamples.Write(context));

        text.ShouldContain("Ann Example");
        text.ShouldContain("Software Architect");
        text.ShouldContain("Scan for the online CV");
        foreach (var section in outline.Sections)
        {
            text.ShouldContain(outline.Heading(section));
        }

        text.ShouldContain("March 2015 – October 2022");
        text.ShouldContain("Shipped");
    }

    [Fact]
    public void WritesAvailabilityDateGivenFromStatus()
    {
        var english = SampleDocuments.English();
        var person = english.Person with
        {
            Availability = new Availability(AvailabilityStatus.From, PartialDate.Parse("2027-05"), "https://example.com/capacity", "Available from"),
        };
        var document = english with { Person = person };

        PdfSamples.Text(PdfSamples.Write(PdfSamples.Context(document, DocumentVariant.Designed))).ShouldContain("Available from (May 2027)");
    }

    [Fact]
    public void WritesStandardHeadingsNumericDatesAndLevelWordsGivenAtsLayout()
    {
        var text = PdfSamples.Text(PdfSamples.Write(DocumentVariant.Ats));

        text.ShouldContain("Summary");
        text.ShouldContain("Skills");
        text.ShouldContain("Work Experience");
        text.ShouldContain("Certifications");
        text.ShouldContain("03/2015 – 10/2022");
        text.ShouldContain("Expert: C#, LINQ");
        text.ShouldContain("Full-stack delivery: End to end.");
        text.ShouldContain("Technologies: C#, Azure");
        text.ShouldContain("Phone: +36 30 123 4567");
        text.ShouldNotContain("Scan for the online CV");
    }

    [Theory]
    [InlineData(DocumentVariant.Designed)]
    [InlineData(DocumentVariant.Ats)]
    public void ExtractsAccentedLettersGivenHungarianAndCroatianNames(DocumentVariant variant)
    {
        var text = PdfSamples.Text(PdfSamples.Write(PdfSamples.Context(SampleDocuments.Accented(), variant)));

        text.ShouldContain("Ann Sípos");
        text.ShouldContain("Győr, Hűvösvölgy; Čačak, Pašman, Ivanić-Grad, Đurđevac, Požega");
    }

    // Plex has fi and fl ligatures; with them on, the text layer would hold one ligature glyph instead of two letters.
    // Without a role the organization is the heading; its note must come with it, as in the designed PDF.
    [Fact]
    public void KeepsNoteGivenAtsPositionWithoutRole()
    {
        var english = SampleDocuments.English();
        var document = english with { Positions = [english.Positions[0] with { Role = null, Note = "formerly a sole proprietorship" }] };

        PdfSamples.Text(PdfSamples.Write(PdfSamples.Context(document, DocumentVariant.Ats))).ShouldContain("formerly a sole proprietorship");
    }

    [Fact]
    public void KeepsLettersOfLigaturesGivenFiInText()
    {
        var document = SampleDocuments.English() with { Person = SampleDocuments.English().Person with { Tagline = "Unified office workflows" } };

        var text = PdfSamples.Text(PdfSamples.Write(PdfSamples.Context(document, DocumentVariant.Designed)));

        text.ShouldContain("Unified office workflows");
        text.ShouldNotContain("ﬁ");
        text.ShouldNotContain("ﬂ");
    }

    [Theory]
    [InlineData(DocumentVariant.Designed, null)]
    [InlineData(DocumentVariant.Designed, PdfSamples.Address)]
    [InlineData(DocumentVariant.Ats, null)]
    [InlineData(DocumentVariant.Ats, PdfSamples.Address)]
    public void WritesNoAtSignGivenAnyContactEmail(DocumentVariant variant, string? email) =>
        PdfSamples.Text(PdfSamples.Write(variant, email)).ShouldNotContain('@');

    [Theory]
    [InlineData(DocumentVariant.Designed)]
    [InlineData(DocumentVariant.Ats)]
    public void AddsOneImageToFirstPageGivenContactEmail(DocumentVariant variant)
    {
        var without = PdfSamples.ImageCount(PdfSamples.Write(variant));

        PdfSamples.ImageCount(PdfSamples.Write(variant, PdfSamples.Address)).ShouldBe(without + 1);
    }

    [Theory]
    [InlineData(DocumentVariant.Designed)]
    [InlineData(DocumentVariant.Ats)]
    public void KeepsAddressOutOfTextLinksMetadataAndBytesGivenContactEmail(DocumentVariant variant)
    {
        var pdf = PdfSamples.Write(variant, PdfSamples.Address);
        var (information, xmp) = PdfSamples.Metadata(pdf);

        PdfSamples.Text(pdf).ShouldNotContain("privacy.probe");
        PdfSamples.LinkTargets(pdf).ShouldNotContain(target => target.Contains("mailto:", StringComparison.OrdinalIgnoreCase) || target.Contains('@'));
        information.DocumentInformationDictionary.ShouldNotBeNull().Data.Values.OfType<StringToken>().ShouldNotContain(value => value.Data.Contains('@'));
        xmp.ShouldNotBeEmpty();
        xmp.ShouldNotContain("@");
        foreach (var part in new[] { PdfSamples.Address, "privacy.probe" })
        {
            PdfSamples.RawAndInflated(pdf).ShouldNotContain(bytes => PdfSamples.Contains(bytes, part), $"'{part}' is in the file");
        }
    }

    [Fact]
    public void DrawsNoImageGivenAtsLayoutWithoutContactEmail()
    {
        var pdf = PdfSamples.Write(DocumentVariant.Ats);
        using var document = PdfDocument.Open(pdf);

        document.GetPages().Sum(page => page.GetImages().Count()).ShouldBe(0);
    }

    [Fact]
    public void DrawsOnlyQrCodeAsVectorGivenDesignedLayoutWithoutContactEmail() =>
        PdfSamples.ImageCount(PdfSamples.Write(DocumentVariant.Designed)).ShouldBe(0);

    [Theory]
    [InlineData("en")]
    [InlineData("hu")]
    public void SetsDocumentLanguageGivenCvLanguage(string tag)
    {
        var document = tag == "hu" ? SampleDocuments.Hungarian() : SampleDocuments.English();

        PdfSamples.CatalogString(PdfSamples.Write(PdfSamples.Context(document, DocumentVariant.Designed)), "Lang").ShouldBe(tag);
    }

    [Theory]
    [InlineData(DocumentVariant.Designed)]
    [InlineData(DocumentVariant.Ats)]
    public void EmbedsPlexFontsGivenAnyLayout(DocumentVariant variant)
    {
        var fonts = PdfSamples.Fonts(PdfSamples.Write(variant));

        fonts.ShouldNotBeEmpty();
        fonts.ShouldAllBe(font => font.Embedded && font.Name.Contains("IBMPlex"));
    }

    [Fact]
    public void UsesPlexMonoForTechnologiesGivenDesignedLayout() =>
        PdfSamples.Fonts(PdfSamples.Write(DocumentVariant.Designed)).ShouldContain(font => font.Name.Contains("IBMPlexMono"));

    [Theory]
    [InlineData(DocumentVariant.Designed)]
    [InlineData(DocumentVariant.Ats)]
    public void WritesOneOrTwoA4PagesGivenSmallCv(DocumentVariant variant)
    {
        using var document = PdfDocument.Open(PdfSamples.Write(variant));

        document.NumberOfPages.ShouldBeInRange(1, 2);
        document.GetPage(1).Width.ShouldBe(595.28, 0.5);
        document.GetPage(1).Height.ShouldBe(841.89, 0.5);
    }

    [Fact]
    public void LinksPhoneOnlineCvContactAndProfilesGivenDesignedLayout() =>
        PdfSamples.LinkTargets(PdfSamples.Write(DocumentVariant.Designed)).ShouldBe(
            ["tel:+36301234567", "https://cv.example.com/", "https://example.com/contact", "https://github.com/ann", "https://example.com/capacity"],
            ignoreOrder: true);

    [Fact]
    public void WritesOnlineCvWithoutSchemeGivenPageUrl() =>
        PdfSamples.Text(PdfSamples.Write(DocumentVariant.Designed)).ShouldContain("cv.example.com");

    [Fact]
    public void ShowsFocusAndFocusSummaryGivenTailoredDocument()
    {
        var english = SampleDocuments.English();
        var document = english with { FocusProfiles = [english.FocusProfiles[0] with { Summary = "Designs systems." }] };

        var pdf = PdfSamples.Write(PdfSamples.Context(document, DocumentVariant.Designed, focusId: "architect"));

        PdfSamples.Text(pdf).ShouldContain("Focus: Software architect");
        PdfSamples.Text(pdf).ShouldContain("Designs systems.");
        PdfSamples.Metadata(pdf).Information.Title.ShouldBe("Ann Example – CV – Software architect");
    }

    [Fact]
    public void WritesTitleAuthorSubjectKeywordsAndContentDateGivenCv()
    {
        var (information, xmp) = PdfSamples.Metadata(PdfSamples.Write(DocumentVariant.Designed));

        information.Title.ShouldBe("Ann Example – CV");
        information.Author.ShouldBe("Ann Example");
        information.Subject.ShouldBe("Software Architect");
        information.Keywords!.Split(", ").ShouldBe(["C#", "Azure", "Rust"], ignoreOrder: true);
        information.GetCreatedDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero));
        information.GetModifiedDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero));
        xmp.ShouldContain("<pdfuaid:part>1</pdfuaid:part>");
        xmp.ShouldContain("<pdfaid:part>3</pdfaid:part>");
    }

    [Theory]
    [InlineData(DocumentVariant.Designed, null)]
    [InlineData(DocumentVariant.Designed, PdfSamples.Address)]
    [InlineData(DocumentVariant.Ats, PdfSamples.Address)]
    public void WritesIdenticalBytesGivenSameContext(DocumentVariant variant, string? email)
    {
        var context = PdfSamples.Context(SampleDocuments.English(), variant, email);

        PdfSamples.Write(context).ShouldBe(PdfSamples.Write(context));
    }

    [Fact]
    public void WritesDifferentIdentifiersGivenDifferentContent()
    {
        var first = PdfSamples.Metadata(PdfSamples.Write(DocumentVariant.Designed)).Xmp;
        var second = PdfSamples.Metadata(PdfSamples.Write(DocumentVariant.Ats)).Xmp;

        DocumentId(first).ShouldNotBe(DocumentId(second));
    }

    [Fact]
    public void FallsBackToFixedDateGivenUndatedContent()
    {
        var document = SampleDocuments.English() with { LastModified = null };

        PdfSamples.Metadata(PdfSamples.Write(PdfSamples.Context(document, DocumentVariant.Ats))).Information.GetCreatedDateTimeOffset()
            .ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public void RejectsDownloadGivenAnotherFormat()
    {
        var context = PdfSamples.Context(SampleDocuments.English(), DocumentVariant.Designed);

        Should.Throw<ArgumentException>(() => PdfSamples.Writer.Write(context with { Download = context.Download with { Format = DownloadFormat.Docx } }, Stream.Null));
    }

    [Fact]
    public void SupportsBothLayoutsOfPdf()
    {
        PdfSamples.Writer.Format.ShouldBe(DownloadFormat.Pdf);
        PdfSamples.Writer.Supports(DocumentVariant.Designed).ShouldBeTrue();
        PdfSamples.Writer.Supports(DocumentVariant.Ats).ShouldBeTrue();
    }

    private static string DocumentId(string xmp)
    {
        var start = xmp.IndexOf("<xmpMM:DocumentID>", StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        return xmp.Substring(start, 60);
    }
}
