using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Documents.Pdf.Fonts;
using Sipos.Resume.Documents.Pdf.Marks;

namespace Sipos.Resume.Documents.Pdf.Layouts;

/// <summary>The PDF for applicant tracking systems: one column of plain text in reading order.</summary>
/// <remarks>
/// <para>
/// Decision: no page header or footer, no tables, columns, colours, meters or QR code; standard headings, text
/// bullets, numeric dates and levels in words.
/// Why: parsers read the text layer in drawing order; they skip or splice header and footer text into the first entry,
/// read table cells and side columns out of order, and know Work Experience but not a designed heading. Every fact the
/// designed PDF shows with a mark is written out here.
/// </para>
/// <para>
/// Decision: the e-mail address, when configured, is the one image.
/// Why: a recruiter who reads this file still needs a way to write; the parser cannot read the address, which is the
/// point of drawing it.
/// </para>
/// </remarks>
internal sealed class AtsLayout : IDocument
{
    private const string Ink = "111111";
    private const string Muted = "444444";
    private const string Rule = "B0B0B0";
    private const float BodySize = 10f;
    private const float BodyLineHeight = 1.3f;
    private const float ContactSize = 9.5f;

    private readonly DocumentContext _context;
    private readonly DocumentOutline _outline;
    private readonly EmailImage? _email;

    /// <summary>Prepares the layout of a document; draws the e-mail image when the context has an address.</summary>
    /// <param name="context">The document's context.</param>
    /// <param name="outline">The document's outline.</param>
    public AtsLayout(DocumentContext context, DocumentOutline outline)
    {
        _context = context;
        _outline = outline;
        PlexFonts.EnsureRegistered();
        _email = string.IsNullOrWhiteSpace(context.ContactEmail)
            ? null
            : EmailImage.Draw(context.ContactEmail.Trim(), PlexFonts.Sans, ContactSize, BodyLineHeight, Ink);
    }

    private Person Person => _context.Document.Person;

    /// <inheritdoc/>
    public DocumentMetadata GetMetadata() => PdfMetadata.Of(_context, _outline);

    /// <inheritdoc/>
    public DocumentSettings GetSettings() => PdfMetadata.Settings();

    /// <inheritdoc/>
    public void Compose(IDocumentContainer container) => container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(54);
        page.MarginVertical(48);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(style => style
            .FontFamily(PlexFonts.Sans)
            .FontSize(BodySize)
            .LineHeight(BodyLineHeight)
            .FontColor(Color.FromHex(Ink))
            .DisableFontFeature(FontFeatures.StandardLigatures));
        page.Content().Column(column =>
        {
            column.Item().Element(Header);
            foreach (var section in _outline.Sections)
            {
                column.Item().PaddingTop(14).EnsureSpace(70).Element(container => Section(container, section));
            }
        });
    });

    private static TextSpanDescriptor Secondary(TextSpanDescriptor span) => span.FontColor(Color.FromHex(Muted));

    private void Header(IContainer container) => container.Column(column =>
    {
        column.Item().Heading(HeadingLevel.Name).Text(Person.Name).FontSize(18).SemiBold().LineHeight(1.2f);
        if (Person.Title is { } title)
        {
            column.Item().Text(title).FontSize(11.5f);
        }

        if (Person.Tagline is { } tagline)
        {
            column.Item().Text(tagline).FontColor(Color.FromHex(Muted));
        }

        if (_outline.Focus is { } focus)
        {
            column.Item().Text($"{_outline.Labels.Focus}: {focus.Profile.Label}");
        }

        // A text separator between the items keeps a parser reading them as one line, not as columns.
        column.Item().PaddingTop(6).Inlined(line =>
        {
            line.VerticalSpacing(2);
            line.BaselineTop();
            var items = ContactItem.Of(_context, _outline).Where(item => item.Kind != ContactKind.Availability).ToList();
            for (var index = 0; index < items.Count; index++)
            {
                if (index > 0)
                {
                    line.Item().PaddingHorizontal(6).SemanticIgnore().Text("|").FontSize(ContactSize).FontColor(Color.FromHex(Muted));
                }

                Contact(line.Item(), items[index]);
            }
        });
    });

    private void Contact(IContainer container, ContactItem item)
    {
        if (item.Kind == ContactKind.Email)
        {
            if (_email is { } email)
            {
                container.SemanticImage(item.Text).Width(email.Width).Height(email.Height).Image(email.Png);
            }

            return;
        }

        var value = item.Kind == ContactKind.Contact && item.Url is { } contact ? ContactItem.WithoutScheme(contact) : item.Text;
        var text = item.Label is { } label ? $"{label}: {value}" : value;
        (item.Url is { } url ? container.SemanticLink(text).Hyperlink(url) : container).Text(text).FontSize(ContactSize);
    }

    private void Section(IContainer container, DocumentSection section) => container.SemanticSection().Column(column =>
    {
        column.Item().Heading(HeadingLevel.Section).Text(_outline.Heading(section)).FontSize(12).SemiBold();
        column.Item().PaddingTop(1).PaddingBottom(6).LineHorizontal(0.5f).LineColor(Color.FromHex(Rule));
        column.Item().Element(container => SectionContent(container, section));
    });

    private void SectionContent(IContainer container, DocumentSection section)
    {
        switch (section)
        {
            case DocumentSection.Profile:
                container.Column(column =>
                {
                    column.Spacing(4);
                    if (_outline.Summary is { } summary)
                    {
                        column.Item().SemanticParagraph().Text(summary);
                    }

                    if (_outline.Strengths.Count > 0)
                    {
                        column.Item().Element(item => Bullets(item, _outline.Strengths.Select(strength =>
                            strength.Summary is { } text ? $"{strength.Title}: {text}" : strength.Title)));
                    }
                });
                break;
            case DocumentSection.Skills:
                Skills(container);
                break;
            case DocumentSection.Experience:
                Entries(container, _context.Document.Positions, Position);
                break;
            case DocumentSection.Projects:
                Entries(container, _context.Document.Projects, (item, engagement) => Engagement(item, engagement, HeadingLevel.Entry));
                break;
            case DocumentSection.Education:
                Entries(container, _context.Document.Education, Study);
                break;
            case DocumentSection.Certificates:
                Bullets(container, _context.Document.Certificates.Select(certificate =>
                    Joined(certificate.Name, JoinedOrNull(certificate.Issuer, certificate.Date is { } date ? _outline.Date(date) : null))));
                break;
            case DocumentSection.Awards:
                Bullets(container, _context.Document.Awards.Select(award =>
                {
                    var line = Joined(award.Title, JoinedOrNull(award.Awarder, award.Date is { } date ? _outline.Date(date) : null));
                    return award.Summary is { } summary ? $"{line}. {summary}" : line;
                }));
                break;
            default:
                Bullets(container, _context.Document.Languages.Select(language => Joined(language.Name, language.Fluency)));
                break;
        }
    }

    private static void Entries<T>(IContainer container, IEnumerable<T> items, Action<IContainer, T> entry) => container.Column(column =>
    {
        column.Spacing(10);
        foreach (var item in items)
        {
            column.Item().Element(container => entry(container, item));
        }
    });

    private void Position(IContainer container, Position position) => container.EnsureSpace(60).Column(column =>
    {
        // Without a role the organization, note included, is the heading, so the note is never lost.
        var organization = position.Note is { } note ? $"{position.Organization} ({note})" : position.Organization;
        column.Item().Heading(HeadingLevel.Entry).Text(position.Role ?? organization).FontSize(10.5f).SemiBold();
        column.Item().Text(text =>
        {
            Secondary(text.Span(string.Join(" | ", new[] { position.Role is null ? null : organization, position.Location, _outline.Period(position.Period) }.OfType<string>())));
        });
        Details(column, position.Summary, _outline.Highlights(position), position.Keywords);
        if (position.Engagements.Count > 0)
        {
            column.Item().PaddingTop(6).Heading(HeadingLevel.Group).Text(_outline.Labels.ClientProjects).SemiBold();
            column.Item().PaddingTop(3).PaddingLeft(10).Column(projects =>
            {
                projects.Spacing(7);
                foreach (var engagement in position.Engagements)
                {
                    projects.Item().Element(item => Engagement(item, engagement, HeadingLevel.NestedEntry));
                }
            });
        }
    });

    private void Engagement(IContainer container, Engagement engagement, HeadingLevel level) => container.EnsureSpace(40).Column(column =>
    {
        column.Item().Heading(level).Text(Joined(engagement.Name, engagement.Client, " – ")).SemiBold();
        var facts = string.Join(" | ", new[] { engagement.Roles.Count > 0 ? string.Join(", ", engagement.Roles) : null, engagement.Period is { } period ? _outline.Period(period) : null }.OfType<string>());
        if (facts.Length > 0)
        {
            column.Item().Text(text => Secondary(text.Span(facts)));
        }

        Details(column, engagement.Description, _outline.Highlights(engagement), engagement.Keywords);
    });

    private void Study(IContainer container, Study study) => container.PreventPageBreak().Column(column =>
    {
        var degree = string.Join(", ", new[] { study.StudyType, study.Area }.OfType<string>());
        column.Item().Heading(HeadingLevel.Entry).Text(degree.Length > 0 ? degree : study.Institution).SemiBold();
        var facts = string.Join(" | ", new[] { degree.Length > 0 ? study.Institution : null, study.Period is { } period ? _outline.Period(period) : null }.OfType<string>());
        if (facts.Length > 0)
        {
            column.Item().Text(text => Secondary(text.Span(facts)));
        }
    });

    private void Details(ColumnDescriptor column, string? summary, IReadOnlyList<string> highlights, IReadOnlyList<string> keywords)
    {
        if (summary is not null)
        {
            column.Item().PaddingTop(2).SemanticParagraph().Text(summary);
        }

        if (highlights.Count > 0)
        {
            column.Item().PaddingTop(2).Element(item => Bullets(item, highlights));
        }

        if (keywords.Count > 0)
        {
            column.Item().PaddingTop(2).SemanticParagraph().Text($"{_outline.Labels.Technologies}: {string.Join(", ", keywords)}");
        }
    }

    // Each group is a line of its levels from the highest: ".NET platform — Expert: C#, LINQ; Proficient: Blazor".
    private void Skills(IContainer container) => container.SemanticList().Column(column =>
    {
        column.Spacing(3);
        foreach (var group in _outline.SkillGroups)
        {
            var levels = group.Skills
                .GroupBy(skill => _outline.Level(skill) ?? "")
                .OrderByDescending(level => level.Max(skill => skill.Rating ?? 0))
                .Select(level => level.Key.Length > 0
                    ? $"{level.Key}: {string.Join(", ", level.Select(skill => skill.Name))}"
                    : string.Join(", ", level.Select(skill => skill.Name)));
            column.Item().PreventPageBreak().SemanticListItem().SemanticListItemBody().Text(text =>
            {
                text.Span($"{group.Name}: ").SemiBold();
                text.Span(string.Join("; ", levels));
            });
        }
    });

    private static void Bullets(IContainer container, IEnumerable<string> items) => container.SemanticList().Column(list =>
    {
        list.Spacing(2);
        foreach (var item in items)
        {
            list.Item().PreventPageBreak().SemanticListItem().Row(row =>
            {
                row.ConstantItem(12).SemanticListLabel().Text("•");
                row.RelativeItem().SemanticListItemBody().Text(item);
            });
        }
    });

    private static string Joined(string first, string? second, string separator = " — ") => second is null ? first : $"{first}{separator}{second}";

    private static string? JoinedOrNull(string? first, string? second, string separator = ", ") =>
        first is null ? second : second is null ? first : $"{first}{separator}{second}";
}
