using System.IO.Compression;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using Sipos.Resume.Core.Artifacts;
using static Sipos.Resume.Generation.Tests.Documents.Docx.DocxTestFiles;

namespace Sipos.Resume.Generation.Tests.Documents.Docx;

public class DocxDocumentWriterWrite
{
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace Cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
    private static readonly XNamespace Terms = "http://purl.org/dc/terms/";

    public static TheoryData<string, DocumentVariant, string?> Documents => new()
    {
        { "en", DocumentVariant.Designed, null },
        { "en", DocumentVariant.Ats, null },
        { "en", DocumentVariant.Designed, "architect" },
        { "hu", DocumentVariant.Designed, null },
        { "hu", DocumentVariant.Ats, null },
        { "hu", DocumentVariant.Designed, "architect" },
        { "sr", DocumentVariant.Designed, null },
        { "sr", DocumentVariant.Ats, null },
    };

    [Theory]
    [MemberData(nameof(Documents))]
    public void PassesOpenXmlValidationGivenEveryLayoutAndLanguage(string language, DocumentVariant variant, string? focus)
    {
        using var document = Open(Write(language, variant, focus));

        new OpenXmlValidator(FileFormatVersions.Microsoft365).Validate(document, TestContext.Current.CancellationToken)
            .Select(error => $"{error.Part?.Uri} {error.Path?.XPath}: {error.Description}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void WritesNameSectionsPeriodsAndHighlightsGivenDesignedLayout()
    {
        var context = Context(SampleDocuments.English(), DocumentVariant.Designed);
        var bytes = Write(context);
        var outline = DocumentOutline.Of(context);

        Texts(bytes, "Title").ShouldBe(["Ann Example"]);
        Texts(bytes, "Subtitle").ShouldBe(["Software Architect"]);
        Texts(bytes, "Heading1").ShouldBe(outline.Sections.Select(outline.Heading));
        Texts(bytes, "Heading2").ShouldContain(text => text.StartsWith("Initech", StringComparison.Ordinal) && text.EndsWith("March 2015 – October 2022", StringComparison.Ordinal));
        Texts(bytes, "ListBullet").ShouldContain("Leads");
        Texts(bytes, "ListBullet").ShouldContain("Full-stack delivery – End to end.");
    }

    // Applicant tracking systems know the standard headings, read digits in dates and levels as words.
    [Fact]
    public void WritesStandardHeadingsNumericDatesAndSkillsInWordsGivenAtsLayout()
    {
        var bytes = Write("en", DocumentVariant.Ats);
        var paragraphs = Paragraphs(bytes).Select(paragraph => paragraph.Text).ToList();

        Texts(bytes, "Heading1").ShouldBe(["Summary", "Skills", "Work Experience", "Projects", "Education", "Certifications", "Awards", "Languages"]);
        Texts(bytes, "Heading2").ShouldContain("Engineer – Initech");
        paragraphs.ShouldContain("03/2015 – 10/2022");
        paragraphs.ShouldContain("Expert: C#, LINQ");
        paragraphs.ShouldContain("Proficient: Blazor");
        paragraphs.ShouldContain("Technologies: C#, Azure");
        Texts(bytes, "ListBullet").ShouldContain("Full-stack delivery: End to end.");
    }

    [Theory]
    [InlineData("hu", "hu-HU", "Távmunkában, Európából", "Szakmai tapasztalat")]
    [InlineData("sr", "sr-Latn-RS", "Đorđe Šćepanović", "Gradi .NET sisteme: čak i žičane mreže.")]
    public void KeepsAccentedLettersAndSetsLanguageGivenCvInAnotherLanguage(string language, string tag, string fragment, string paragraph)
    {
        var bytes = Write(language, DocumentVariant.Designed);
        var texts = Paragraphs(bytes).Select(item => item.Text).ToList();

        texts.ShouldContain(text => text.Contains(fragment, StringComparison.Ordinal));
        texts.ShouldContain(paragraph);
        Part(bytes, "word/styles.xml").Descendants(W + "docDefaults").Descendants(W + "lang").Single().Attribute(W + "val")!.Value.ShouldBe(tag);
    }

    [Fact]
    public void UsesHeadingStylesForSectionsPositionsAndProjects()
    {
        var bytes = Write("en", DocumentVariant.Designed);
        var styles = Part(bytes, "word/styles.xml").Descendants(W + "style").ToDictionary(style => style.Attribute(W + "styleId")!.Value);

        Texts(bytes, "Heading2").ShouldContain(text => text.StartsWith("acme", StringComparison.Ordinal));
        Texts(bytes, "Heading2").ShouldContain("Hobby");
        Texts(bytes, "Heading3").ShouldContain(text => text.StartsWith("Portal – Globex", StringComparison.Ordinal));
        styles["Heading1"].Element(W + "name")!.Attribute(W + "val")!.Value.ShouldBe("heading 1");
        styles["Heading2"].Descendants(W + "outlineLvl").Single().Attribute(W + "val")!.Value.ShouldBe("1");
    }

    [Theory]
    [InlineData(DocumentVariant.Designed, "∧")]
    [InlineData(DocumentVariant.Ats, "•")]
    public void BindsBulletsToNumberingGivenLayout(DocumentVariant variant, string bullet)
    {
        var bytes = Write("en", variant);
        var listBullet = Part(bytes, "word/styles.xml").Descendants(W + "style").Single(style => style.Attribute(W + "styleId")!.Value == "ListBullet");
        var numId = listBullet.Descendants(W + "numId").Single().Attribute(W + "val")!.Value;
        var numbering = Part(bytes, "word/numbering.xml");
        var abstractId = numbering.Descendants(W + "num").Single(num => num.Attribute(W + "numId")!.Value == numId).Element(W + "abstractNumId")!.Attribute(W + "val")!.Value;
        var level = numbering.Descendants(W + "abstractNum").Single(item => item.Attribute(W + "abstractNumId")!.Value == abstractId).Element(W + "lvl")!;

        level.Element(W + "numFmt")!.Attribute(W + "val")!.Value.ShouldBe("bullet");
        level.Element(W + "lvlText")!.Attribute(W + "val")!.Value.ShouldBe(bullet);
        level.Element(W + "pStyle")!.Attribute(W + "val")!.Value.ShouldBe("ListBullet");
        Texts(bytes, "ListBullet").ShouldContain("Shipped");
    }

    // A DOCX is edited and forwarded out of the owner's hands; the build's address is for the PDF alone.
    [Theory]
    [InlineData(DocumentVariant.Designed, null)]
    [InlineData(DocumentVariant.Ats, null)]
    [InlineData(DocumentVariant.Designed, "architect")]
    public void WritesNoEmailAddressGivenContactEmail(DocumentVariant variant, string? focus)
    {
        var bytes = Write(Context(SampleDocuments.English(), variant, focus, email: "ann@example.com"));

        foreach (var (name, text) in Parts(bytes))
        {
            text.ShouldNotContain("@", customMessage: name);
            text.ShouldNotContain("mailto", Case.Insensitive, customMessage: name);
        }
    }

    [Fact]
    public void UsesNoTablesShapesHeadersOrExtraSectionsGivenAtsLayout()
    {
        var bytes = Write("en", DocumentVariant.Ats);
        var body = Part(bytes, "word/document.xml");
        XNamespace vml = "urn:schemas-microsoft-com:vml";

        body.Descendants(W + "tbl").ShouldBeEmpty();
        body.Descendants(W + "drawing").ShouldBeEmpty();
        body.Descendants(W + "pict").ShouldBeEmpty();
        body.Descendants(W + "txbxContent").ShouldBeEmpty();
        body.Descendants(vml + "shape").ShouldBeEmpty();
        body.Descendants(W + "sectPr").Count().ShouldBe(1);
        body.Descendants(W + "cols").ShouldBeEmpty();
        using var document = Open(bytes);
        document.MainDocumentPart!.HeaderParts.ShouldBeEmpty();
        document.MainDocumentPart.FooterParts.ShouldBeEmpty();
    }

    [Fact]
    public void NamesOnlyCalibriAndArialGivenAtsLayout()
    {
        var bytes = Write("en", DocumentVariant.Ats);

        Fonts(bytes).ShouldBe(["Arial", "Calibri"], ignoreOrder: true);
        Part(bytes, "word/fontTable.xml").Descendants(W + "font").Single(font => font.Attribute(W + "name")!.Value == "Calibri")
            .Element(W + "altName")!.Attribute(W + "val")!.Value.ShouldBe("Arial");
    }

    [Fact]
    public void UsesPlexFontsAndThemeColoursGivenDesignedLayout()
    {
        var bytes = Write("en", DocumentVariant.Designed);
        var styles = Part(bytes, "word/styles.xml");
        var colours = styles.Descendants(W + "color").Concat(Part(bytes, "word/numbering.xml").Descendants(W + "color"))
            .Select(color => color.Attribute(W + "val")!.Value).ToHashSet();

        Fonts(bytes).ShouldContain("IBM Plex Sans");
        Fonts(bytes).ShouldContain("IBM Plex Mono");
        new[] { Theme.Heading, Theme.Body, Theme.Muted, Theme.Subtle, Theme.Accent }.ShouldBeSubsetOf(colours);
        styles.Descendants(W + "style").Single(style => style.Attribute(W + "styleId")!.Value == "Heading1")
            .Descendants(W + "bottom").Single().Attribute(W + "color")!.Value.ShouldBe(Theme.Rule);
    }

    [Fact]
    public void DrawsLevelMetersInAccentAndSubtleGivenDesignedLayout()
    {
        var bytes = Write("en", DocumentVariant.Designed);
        using var document = Open(bytes);
        var cells = document.MainDocumentPart!.Document!.Body!.Descendants<TableCell>().Select(cell => cell.InnerText).ToList();
        var styles = Part(bytes, "word/styles.xml").Descendants(W + "style").ToDictionary(style => style.Attribute(W + "styleId")!.Value);

        cells.ShouldContain("●●●●●  Expert");
        cells.ShouldContain("●●●○○  Proficient");
        styles["MeterFilled"].Descendants(W + "color").Single().Attribute(W + "val")!.Value.ShouldBe(Theme.Accent);
        styles["MeterEmpty"].Descendants(W + "color").Single().Attribute(W + "val")!.Value.ShouldBe(Theme.Subtle);
    }

    [Fact]
    public void SetsCorePropertiesGivenHungarianCv()
    {
        var core = Part(Write("hu", DocumentVariant.Designed), "docProps/core.xml").Root!;

        core.Element(Dc + "title")!.Value.ShouldBe("Example Ann – Önéletrajz");
        core.Element(Dc + "creator")!.Value.ShouldBe("Example Ann");
        core.Element(Dc + "subject")!.Value.ShouldBe("Szoftverarchitekt");
        core.Element(Dc + "language")!.Value.ShouldBe("hu-HU");
        core.Element(Cp + "keywords")!.Value.Split(", ").ShouldBe(["Azure", "C#", "Rust"], ignoreOrder: true);
        core.Element(Terms + "created")!.Value.ShouldBe("2026-10-07T00:00:00Z");
        core.Element(Terms + "modified")!.Value.ShouldBe("2026-10-07T00:00:00Z");
    }

    [Fact]
    public void KeepsPersonalDataOutOfApplicationProperties()
    {
        var parts = Parts(Write("en", DocumentVariant.Designed));

        parts["docProps/app.xml"].ShouldNotContain("Ann");
        parts["docProps/app.xml"].ShouldNotContain("123 4567");
    }

    [Theory]
    [InlineData(DocumentVariant.Designed, "Get in touch")]
    [InlineData(DocumentVariant.Ats, "example.com/contact")]
    public void LinksOnlineCvContactPageAndProfiles(DocumentVariant variant, string contactText)
    {
        using var document = Open(Write("en", variant));
        var main = document.MainDocumentPart!;
        var links = main.Document!.Body!.Descendants<Hyperlink>()
            .ToDictionary(link => link.InnerText, link => main.HyperlinkRelationships.Single(relationship => relationship.Id == link.Id!.Value).Uri);

        links["cv.example.com/hu"].ShouldBe(PageUrl);
        links[contactText].ShouldBe(new Uri("https://example.com/contact"));
        links["github.com/ann"].ShouldBe(new Uri("https://github.com/ann"));
        main.HyperlinkRelationships.ShouldAllBe(relationship => relationship.IsExternal);
    }

    // An ATS looks for the standard field label; the CV's own words follow it, as in the ATS text and PDF.
    [Fact]
    public void LabelsContactByFieldGivenAtsLayout() =>
        Paragraphs(Write("en", DocumentVariant.Ats)).Select(paragraph => paragraph.Text).ShouldContain(text => text.StartsWith("Contact: Get in touch – example.com/contact", StringComparison.Ordinal));

    // The designed documents say the same: the PDF and the Markdown show the availability, so the DOCX does too.
    [Fact]
    public void LinksAvailabilityGivenDesignedLayout()
    {
        using var document = Open(Write("en", DocumentVariant.Designed));
        var main = document.MainDocumentPart!;

        var link = main.Document!.Body!.Descendants<Hyperlink>().Single(link => link.InnerText == "Available now");
        main.HyperlinkRelationships.Single(relationship => relationship.Id == link.Id!.Value).Uri.ShouldBe(new Uri("https://example.com/capacity"));
    }

    [Fact]
    public void ShowsFocusLabelGivenTailoredDocument()
    {
        var tailored = Write("en", DocumentVariant.Designed, "architect");

        Texts(tailored, "FocusLine").ShouldBe(["Focus: Software architect"]);
        Part(tailored, "docProps/core.xml").Root!.Element(Dc + "title")!.Value.ShouldBe("Ann Example – CV – Software architect");
        Texts(Write("en", DocumentVariant.Designed), "FocusLine").ShouldBeEmpty();
    }

    [Fact]
    public void LeavesOutCommentsRevisionsMacrosAndCustomXml()
    {
        var bytes = Write("en", DocumentVariant.Designed);
        using var document = Open(bytes);
        var main = document.MainDocumentPart!;

        document.DocumentType.ShouldBe(WordprocessingDocumentType.Document);
        main.WordprocessingCommentsPart.ShouldBeNull();
        main.CustomXmlParts.ShouldBeEmpty();
        main.GetPartsOfType<VbaProjectPart>().ShouldBeEmpty();
        main.Document!.Descendants<InsertedRun>().ShouldBeEmpty();
        main.Document.Descendants<DeletedRun>().ShouldBeEmpty();
        Parts(bytes).Keys.ShouldBe(
            ["[Content_Types].xml", "_rels/.rels", "docProps/app.xml", "docProps/core.xml", "word/_rels/document.xml.rels", "word/document.xml", "word/fontTable.xml", "word/numbering.xml", "word/settings.xml", "word/styles.xml"]);
    }

    // System.IO.Packaging stamps entries with the clock; the same commit must publish the same file.
    [Fact]
    public void WritesIdenticalBytesGivenSameContext()
    {
        var context = Context(SampleDocuments.Hungarian(), DocumentVariant.Designed, "architect");

        var first = Write(context);
        var second = Write(context);

        second.ShouldBe(first);
        EntryTimes(first).ShouldAllBe(time => time == new DateTime(2026, 10, 7));
    }

    [Fact]
    public void StampsEntriesWithFirstZipDateGivenNoLastModified()
    {
        var bytes = Write(Context(SampleDocuments.English() with { LastModified = null }, DocumentVariant.Ats));

        EntryTimes(bytes).ShouldAllBe(time => time == new DateTime(1980, 1, 1));
        Part(bytes, "docProps/core.xml").Root!.Element(Terms + "modified")!.Value.ShouldBe("1980-01-01T00:00:00Z");
    }

    private static List<string> Fonts(byte[] bytes)
    {
        string[] attributes = ["ascii", "hAnsi", "cs", "eastAsia"];
        var named = new[] { "word/styles.xml", "word/numbering.xml", "word/document.xml" }
            .SelectMany(part => Part(bytes, part).Descendants(W + "rFonts"))
            .SelectMany(fonts => attributes.Select(attribute => fonts.Attribute(W + attribute)?.Value).OfType<string>());
        var table = Part(bytes, "word/fontTable.xml").Descendants(W + "font").Select(font => font.Attribute(W + "name")!.Value);
        return [.. named.Concat(table).Distinct()];
    }

    private static List<DateTime> EntryTimes(byte[] bytes)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes));
        return [.. zip.Entries.Select(entry => entry.LastWriteTime.DateTime)];
    }
}
