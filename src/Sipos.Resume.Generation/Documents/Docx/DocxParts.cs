using System.Globalization;
using System.Text;
using System.Xml;
using DocumentFormat.OpenXml.Wordprocessing;
using Ap = DocumentFormat.OpenXml.ExtendedProperties;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>What the core properties of a document say.</summary>
/// <param name="Title">The title, such as <c>Ann Example – CV</c>.</param>
/// <param name="Creator">The author.</param>
/// <param name="Subject">The subject, or <see langword="null"/>.</param>
/// <param name="Keywords">The keywords.</param>
/// <param name="Language">The language tag, such as <c>hu-HU</c>.</param>
/// <param name="Stamp">When the content last changed.</param>
internal sealed record DocxCoreProperties(string Title, string Creator, string? Subject, IReadOnlyList<string> Keywords, string Language, DateOnly Stamp);

/// <summary>Builds the parts around the body: settings, font table, core and application properties.</summary>
internal static class DocxParts
{
    private const string CoreNamespace = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
    private const string DcNamespace = "http://purl.org/dc/elements/1.1/";
    private const string TermsNamespace = "http://purl.org/dc/terms/";
    private const string TypesNamespace = "http://purl.org/dc/dcmitype/";
    private const string SchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>The settings: Word 2013 and later layout, so Word opens the file without compatibility mode.</summary>
    public static Settings Settings() => new(
        new Zoom { Percent = "100" },
        new DefaultTabStop { Val = 709 },
        new CharacterSpacingControl { Val = CharacterSpacingValues.DoNotCompress },
        new Compatibility(new CompatibilitySetting
        {
            Name = CompatSettingNameValues.CompatibilityMode,
            Uri = "http://schemas.microsoft.com/office/word",
            Val = "15",
        }));

    public static Fonts FontTable(DocxLook look) => new(look.Fonts.Select(font =>
    {
        var entry = new Font { Name = font.Name };
        if (font.AltName is not null)
        {
            entry.AltName = new AltName { Val = font.AltName };
        }

        if (font.Panose is not null)
        {
            entry.Panose1Number = new Panose1Number { Val = font.Panose };
        }

        entry.FontCharSet = new FontCharSet { Val = "00" };
        entry.FontFamily = new FontFamily { Val = font.Family == "modern" ? FontFamilyValues.Modern : font.Family == "roman" ? FontFamilyValues.Roman : FontFamilyValues.Swiss };
        entry.Pitch = new Pitch { Val = font.Fixed ? FontPitchValues.Fixed : FontPitchValues.Variable };
        return entry;
    }));

    /// <summary>The application properties: the producing application only, nothing about the person.</summary>
    public static Ap.Properties AppProperties()
    {
        var properties = new Ap.Properties(
            new Ap.Application("resume-engine"),
            new Ap.DocumentSecurity("0"),
            new Ap.ScaleCrop("false"),
            new Ap.LinksUpToDate("false"),
            new Ap.SharedDocument("false"),
            new Ap.HyperlinksChanged("false"));
        properties.AddNamespaceDeclaration("vt", "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes");
        return properties;
    }

    /// <summary>Writes <c>docProps/core.xml</c>.</summary>
    /// <remarks>
    /// Decision: the core properties are written by hand into the part the SDK names <c>docProps/core.xml</c>.
    /// Why: <c>System.IO.Packaging</c>'s package properties live in a part with a random GUID name, so two runs would
    /// never produce the same bytes.
    /// </remarks>
    public static void WriteCoreProperties(Stream stream, DocxCoreProperties properties)
    {
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false };
        using var writer = XmlWriter.Create(stream, settings);
        writer.WriteStartDocument(standalone: true);
        writer.WriteStartElement("cp", "coreProperties", CoreNamespace);
        writer.WriteAttributeString("xmlns", "dc", null, DcNamespace);
        writer.WriteAttributeString("xmlns", "dcterms", null, TermsNamespace);
        writer.WriteAttributeString("xmlns", "dcmitype", null, TypesNamespace);
        writer.WriteAttributeString("xmlns", "xsi", null, SchemaInstanceNamespace);
        writer.WriteElementString("dc", "title", DcNamespace, properties.Title);
        if (properties.Subject is not null)
        {
            writer.WriteElementString("dc", "subject", DcNamespace, properties.Subject);
        }

        writer.WriteElementString("dc", "creator", DcNamespace, properties.Creator);
        writer.WriteElementString("cp", "keywords", CoreNamespace, string.Join(", ", properties.Keywords));
        writer.WriteElementString("dc", "language", DcNamespace, properties.Language);
        var stamp = properties.Stamp.ToString("yyyy-MM-dd'T00:00:00Z'", CultureInfo.InvariantCulture);
        foreach (var name in new[] { "created", "modified" })
        {
            writer.WriteStartElement("dcterms", name, TermsNamespace);
            writer.WriteAttributeString("xsi", "type", SchemaInstanceNamespace, "dcterms:W3CDTF");
            writer.WriteString(stamp);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }
}
