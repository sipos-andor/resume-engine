using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Documents.Pdf.Fonts;
using Sipos.Resume.Documents.Pdf.Marks;

namespace Sipos.Resume.Documents.Pdf.Layouts;

/// <summary>The designed PDF: the theme's colours and fonts, chevron bullets, level meters and a QR code to the online CV.</summary>
/// <remarks>
/// Decision: one main column, dates on the right of each entry, thin rules between sections.
/// Why: a CV is read top to bottom and printed; a side column halves the line length of the highlights, which carry
/// most of the text, and splits badly across pages.
/// </remarks>
internal sealed class DesignedLayout : IDocument
{
    private const float PageMargin = 46;
    private const float ContentWidth = 595.28f - 2 * PageMargin;
    private const float BodySize = 9f;
    private const float BodyLineHeight = 1.42f;
    private const float MetaSize = 8.2f;
    private const float MonoSize = 7.6f;
    private const float ContactSize = 8.4f;
    private const float ContactLineHeight = 1.3f;
    private const float BulletIndent = 12;
    private const float ChevronWidth = 5.6f;
    private const float ChevronTop = 4.9f;
    private const float QrSize = 60;
    private const float QrBox = 88;
    private const float SkillGap = 26;
    private const float SkillWidth = (int)((ContentWidth - SkillGap) / 2);
    private const float MeterHeight = 4.4f;
    private const float LevelWidth = 58;

    private readonly DocumentContext _context;
    private readonly DocumentOutline _outline;
    private readonly DocumentTheme _theme;
    private readonly EmailImage? _email;
    private readonly string _chevron;
    private readonly Dictionary<int, string> _meters = [];

    /// <summary>Prepares the layout of a document; draws the e-mail image when the context has an address.</summary>
    /// <param name="context">The document's context.</param>
    /// <param name="outline">The document's outline.</param>
    public DesignedLayout(DocumentContext context, DocumentOutline outline)
    {
        _context = context;
        _outline = outline;
        _theme = PlexFonts.Resolve(context.Theme);
        _email = string.IsNullOrWhiteSpace(context.ContactEmail)
            ? null
            : EmailImage.Draw(context.ContactEmail.Trim(), _theme.SansFamily, ContactSize, ContactLineHeight, _theme.Body);
        _chevron = VectorMarks.Chevron(_theme.Accent);
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
        page.MarginHorizontal(PageMargin);
        page.MarginTop(40);
        page.MarginBottom(26);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(style => style
            .FontFamily(_theme.SansFamily)
            .FontSize(BodySize)
            .LineHeight(BodyLineHeight)
            .FontColor(Ink(_theme.Body))
            .DisableFontFeature(FontFeatures.StandardLigatures));
        page.Content().Column(column =>
        {
            column.Item().Element(Header);
            foreach (var section in _outline.Sections)
            {
                column.Item().PaddingTop(16).EnsureSpace(80).Element(container => Section(container, section));
            }
        });
        page.Footer().Element(Footer);
    });

    private static Color Ink(string hex) => Color.FromHex(hex);

    private void Header(IContainer container) => container.Column(column =>
    {
        column.Item().Row(row =>
        {
            row.RelativeItem().Column(identity =>
            {
                identity.Item().Heading(HeadingLevel.Name).Text(Person.Name).FontSize(24).SemiBold().LineHeight(1.1f).FontColor(Ink(_theme.Heading));
                if (Person.Title is { } title)
                {
                    identity.Item().PaddingTop(4).Text(title).FontSize(12).Medium().LineHeight(1.25f).FontColor(Ink(_theme.Body));
                }

                if (Person.Tagline is { } tagline)
                {
                    identity.Item().PaddingTop(2).Text(tagline).FontSize(9.5f).LineHeight(1.3f).FontColor(Ink(_theme.Muted));
                }

                if (_outline.Focus is { } focus)
                {
                    identity.Item().PaddingTop(6).Text(text =>
                    {
                        text.Span($"{_outline.Labels.Focus}: ").FontColor(Ink(_theme.Muted));
                        text.Span(focus.Profile.Label).SemiBold().FontColor(Ink(_theme.Accent));
                    });
                }

                identity.Item().PaddingTop(12).Element(Contacts);
            });
            row.ConstantItem(QrBox).PaddingLeft(10).Column(qr =>
            {
                var alternative = $"{_outline.Labels.QrCaption}: {ContactItem.WithoutScheme(_context.PageUrl)}";
                qr.Item().AlignRight().SemanticImage(alternative).Width(QrSize).Height(QrSize).Svg(QrCode.Svg(_context.PageUrl, _theme.Heading));
                qr.Item().PaddingTop(4).AlignRight().Text(_outline.Labels.QrCaption).FontSize(6.3f).LineHeight(1.2f).FontColor(Ink(_theme.Muted)).AlignRight();
            });
        });
    });

    // A separator between the items keeps text extraction on one line; spacing alone reads as columns to pdftotext.
    private void Contacts(IContainer container) => container.Inlined(line =>
    {
        line.VerticalSpacing(3);
        line.BaselineTop();
        var items = ContactItem.Of(_context, _outline);
        for (var index = 0; index < items.Count; index++)
        {
            if (index > 0)
            {
                line.Item().PaddingHorizontal(5).SemanticIgnore().Text("·").FontSize(ContactSize).LineHeight(ContactLineHeight).FontColor(Ink(_theme.Subtle));
            }

            Contact(line.Item(), items[index]);
        }
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

        var accent = item.Url is not null || item.Kind == ContactKind.Availability;
        var target = item.Url is { } url ? container.SemanticLink(item.Text).Hyperlink(url) : container;
        var text = target.Text(item.Text).FontSize(ContactSize).LineHeight(ContactLineHeight).FontColor(Ink(accent ? _theme.Accent : _theme.Body));
        if (item.Kind == ContactKind.Availability)
        {
            text.SemiBold();
        }
    }

    private void Footer(IContainer container) => container
        .SemanticIgnore()
        .PaddingTop(8)
        .DefaultTextStyle(style => style.FontSize(7.4f).FontColor(Ink(_theme.Muted)))
        .Row(row =>
        {
            row.RelativeItem().Text(Person.Name);
            row.AutoItem().Text(text =>
            {
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });

    private void Section(IContainer container, DocumentSection section) => container.SemanticSection().Column(column =>
    {
        column.Item().LineHorizontal(0.6f).LineColor(Ink(_theme.Rule));
        column.Item().PaddingTop(9).PaddingBottom(7).Heading(HeadingLevel.Section).Text(_outline.Heading(section)).FontSize(11.5f).SemiBold().LineHeight(1.2f).FontColor(Ink(_theme.Heading));
        column.Item().Element(container => SectionContent(container, section));
    });

    private void SectionContent(IContainer container, DocumentSection section)
    {
        switch (section)
        {
            case DocumentSection.Profile:
                Paragraph(container, _outline.Summary ?? "");
                break;
            case DocumentSection.Strengths:
                Bullets(container, _outline.Strengths, (item, strength) => item.Text(text =>
                {
                    text.Span(strength.Title).SemiBold().FontColor(Ink(_theme.Heading));
                    if (strength.Summary is { } summary)
                    {
                        text.Span($" — {summary}");
                    }
                }));
                break;
            case DocumentSection.Experience:
                container.Column(column =>
                {
                    column.Spacing(14);
                    foreach (var position in _context.Document.Positions)
                    {
                        column.Item().Element(item => Position(item, position));
                    }
                });
                break;
            case DocumentSection.Projects:
                container.Column(column =>
                {
                    column.Spacing(12);
                    foreach (var engagement in _context.Document.Projects)
                    {
                        column.Item().Element(item => Engagement(item, engagement, HeadingLevel.Entry));
                    }
                });
                break;
            case DocumentSection.Skills:
                Skills(container);
                break;
            case DocumentSection.Education:
                Education(container);
                break;
            case DocumentSection.Awards:
                Bullets(container, _context.Document.Awards, (item, award) => Dated(item, award.Date is { } date ? _outline.Date(date) : null, text =>
                {
                    text.Span(award.Title).SemiBold().FontColor(Ink(_theme.Heading));
                    if (award.Awarder is { } awarder)
                    {
                        text.Span($" — {awarder}");
                    }

                    if (award.Summary is { } summary)
                    {
                        text.Span($". {summary}").FontColor(Ink(_theme.Muted));
                    }
                }));
                break;
            default:
                Bullets(container, _context.Document.Languages, (item, language) => item.Text(text =>
                {
                    text.Span(language.Name).SemiBold().FontColor(Ink(_theme.Heading));
                    if (language.Fluency is { } fluency)
                    {
                        text.Span($" — {fluency}");
                    }
                }));
                break;
        }
    }

    private void Position(IContainer container, Position position) => container.EnsureSpace(56).Column(column =>
    {
        column.Item().Element(item => EntryHeading(item, _outline.Period(position.Period), text =>
        {
            text.Span(position.Organization).FontSize(10.5f).SemiBold().FontColor(Ink(_theme.Heading));
            if (position.Note is { } note)
            {
                text.Span($"  {note}").FontSize(MetaSize).FontColor(Ink(_theme.Muted));
            }
        }));
        if (position.Role is not null || position.Location is not null)
        {
            column.Item().Text(text =>
            {
                if (position.Role is { } role)
                {
                    text.Span(role).Medium().FontColor(Ink(_theme.Body));
                }

                if (position.Location is { } location)
                {
                    text.Span(position.Role is null ? location : $" · {location}").FontColor(Ink(_theme.Muted));
                }
            });
        }

        Details(column, position.Summary, _outline.Highlights(position), position.Keywords);
        if (position.Engagements.Count > 0)
        {
            column.Item().PaddingTop(9).PaddingLeft(1).BorderLeft(0.8f).BorderColor(Ink(_theme.Rule)).PaddingLeft(11).Column(projects =>
            {
                projects.Item().PaddingBottom(5).Heading(HeadingLevel.Group).Text(_outline.Labels.ClientProjects.ToUpper(_outline.Labels.Culture))
                    .FontSize(7.2f).SemiBold().LetterSpacing(0.06f).FontColor(Ink(_theme.Muted));
                projects.Spacing(9);
                foreach (var engagement in position.Engagements)
                {
                    projects.Item().Element(item => Engagement(item, engagement, HeadingLevel.NestedEntry));
                }
            });
        }
    });

    private void Engagement(IContainer container, Engagement engagement, HeadingLevel level) => container.EnsureSpace(44).Column(column =>
    {
        column.Item().Element(item => EntryHeading(item, engagement.Period is { } period ? _outline.Period(period) : null, text =>
        {
            text.Span(engagement.Name).FontSize(9.6f).SemiBold().FontColor(Ink(_theme.Heading));
            if (engagement.Client is { } client)
            {
                text.Span($" – {client}").FontSize(9.6f).FontColor(Ink(_theme.Body));
            }
        }, level));
        if (engagement.Roles.Count > 0)
        {
            column.Item().Text(string.Join(", ", engagement.Roles)).Medium().FontSize(8.6f).FontColor(Ink(_theme.Muted));
        }

        Details(column, engagement.Description, _outline.Highlights(engagement), engagement.Keywords);
    });

    private void EntryHeading(IContainer container, string? period, Action<TextDescriptor> title, HeadingLevel level = HeadingLevel.Entry) => container.Row(row =>
    {
        row.RelativeItem().Heading(level).Text(text =>
        {
            text.DefaultTextStyle(style => style.LineHeight(1.3f));
            title(text);
        });
        if (period is not null)
        {
            row.AutoItem().PaddingLeft(12).PaddingTop(1.5f).Text(period).FontSize(MetaSize).LineHeight(1.3f).FontColor(Ink(_theme.Muted));
        }
    });

    private void Details(ColumnDescriptor column, string? summary, IReadOnlyList<string> highlights, IReadOnlyList<string> keywords)
    {
        if (summary is not null)
        {
            column.Item().PaddingTop(3).Element(item => Paragraph(item, summary));
        }

        if (highlights.Count > 0)
        {
            column.Item().PaddingTop(4).Element(item => Bullets(item, highlights, (body, highlight) => body.Text(highlight)));
        }

        if (keywords.Count > 0)
        {
            column.Item().PaddingTop(4).Text(string.Join(", ", keywords)).FontFamily(_theme.MonoFamily).FontSize(MonoSize).LineHeight(1.35f).FontColor(Ink(_theme.Muted));
        }
    }

    private void Skills(IContainer container) => container.Column(column =>
    {
        column.Spacing(9);
        foreach (var group in _outline.SkillGroups)
        {
            column.Item().EnsureSpace(36).Column(block =>
            {
                block.Item().PaddingBottom(3).Heading(HeadingLevel.Entry).Text(group.Name).FontSize(9.4f).SemiBold().FontColor(Ink(_theme.Heading));
                block.Item().SemanticList().Inlined(grid =>
                {
                    grid.HorizontalSpacing(SkillGap);
                    grid.VerticalSpacing(1.5f);
                    foreach (var skill in group.Skills)
                    {
                        grid.Item().Width(SkillWidth).SemanticListItem().SemanticListItemBody().Element(item => Skill(item, skill));
                    }
                });
            });
        }
    });

    private void Skill(IContainer container, Skill skill) => container.Row(row =>
    {
        row.RelativeItem().Text(skill.Name).LineHeight(1.35f);
        if (skill.Rating is { } rating)
        {
            var width = MeterHeight * VectorMarks.MeterRatio;
            row.ConstantItem(width + 8).PaddingTop(3.6f).SemanticIgnore().Width(width).Height(MeterHeight).Svg(Meter(rating));
        }

        if (_outline.Level(skill) is { } level)
        {
            row.ConstantItem(LevelWidth).PaddingTop(0.8f).Text(level).FontSize(7.6f).LineHeight(1.35f).FontColor(Ink(_theme.Muted));
        }
    });

    private string Meter(int rating)
    {
        if (!_meters.TryGetValue(rating, out var svg))
        {
            _meters[rating] = svg = VectorMarks.Meter(rating, _theme.Accent, _theme.Rule);
        }

        return svg;
    }

    private void Education(IContainer container) => container.Column(column =>
    {
        column.Spacing(7);
        foreach (var study in _context.Document.Education)
        {
            column.Item().PreventPageBreak().Element(item => EntryHeading(item, study.Period is { } period ? _outline.Period(period) : null, text =>
            {
                var degree = string.Join(", ", new[] { study.StudyType, study.Area }.OfType<string>());
                if (degree.Length > 0)
                {
                    text.Span(degree).SemiBold().FontColor(Ink(_theme.Heading));
                    text.Span($" — {study.Institution}");
                }
                else
                {
                    text.Span(study.Institution).SemiBold().FontColor(Ink(_theme.Heading));
                }
            }));
        }

        if (_context.Document.Certificates.Count > 0)
        {
            column.Item().PaddingTop(4).Heading(HeadingLevel.Entry).Text(_outline.Labels.Certificates).FontSize(9.4f).SemiBold().FontColor(Ink(_theme.Heading));
            column.Item().Element(item => Bullets(item, _context.Document.Certificates, (body, certificate) =>
                Dated(body, certificate.Date is { } date ? _outline.Date(date) : null, text =>
                {
                    text.Span(certificate.Name);
                    if (certificate.Issuer is { } issuer)
                    {
                        text.Span($" — {issuer}").FontColor(Ink(_theme.Muted));
                    }
                })));
        }
    });

    private void Dated(IContainer container, string? date, Action<TextDescriptor> content) => container.Row(row =>
    {
        row.RelativeItem().Text(content);
        if (date is not null)
        {
            row.AutoItem().PaddingLeft(12).PaddingTop(0.6f).Text(date).FontSize(MetaSize).FontColor(Ink(_theme.Muted));
        }
    });

    private static void Paragraph(IContainer container, string text) => container.SemanticParagraph().Text(text);

    private void Bullets<T>(IContainer container, IEnumerable<T> items, Action<IContainer, T> body) => container.SemanticList().Column(list =>
    {
        list.Spacing(2.5f);
        foreach (var item in items)
        {
            list.Item().PreventPageBreak().SemanticListItem().Row(row =>
            {
                row.ConstantItem(BulletIndent).PaddingTop(ChevronTop).SemanticIgnore()
                    .Width(ChevronWidth).Height(ChevronWidth / VectorMarks.ChevronRatio).Svg(_chevron);
                row.RelativeItem().SemanticListItemBody().Element(container => body(container, item));
            });
        }
    });
}
