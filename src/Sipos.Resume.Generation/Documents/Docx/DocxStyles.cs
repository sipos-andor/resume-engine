using System.Globalization;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>The identifiers of the styles the layouts use.</summary>
internal static class StyleIds
{
    public const string Normal = "Normal";
    public const string Title = "Title";
    public const string Subtitle = "Subtitle";
    public const string Tagline = "Tagline";
    public const string FocusLine = "FocusLine";
    public const string Contact = "Contact";
    public const string Heading1 = "Heading1";
    public const string Heading2 = "Heading2";
    public const string Heading3 = "Heading3";
    public const string ItemMeta = "ItemMeta";
    public const string GroupLabel = "GroupLabel";
    public const string ListBullet = "ListBullet";
    public const string ListContinue = "ListContinue";
    public const string Technologies = "Technologies";
    public const string SkillGroup = "SkillGroup";
    public const string Skill = "Skill";
    public const string TableNormal = "TableNormal";
    public const string DefaultParagraphFont = "DefaultParagraphFont";
    public const string Hyperlink = "Hyperlink";
    public const string Strong = "Strong";
    public const string Muted = "Muted";
    public const string Date = "Date";
    public const string Note = "Note";
    public const string MeterFilled = "MeterFilled";
    public const string MeterEmpty = "MeterEmpty";
}

/// <summary>Builds the style sheet and the bullet numbering of a layout.</summary>
/// <remarks>
/// Decision: every look is a named style (Title, Heading 1–3, List Bullet, Hyperlink…), and the body carries style
/// references only, no direct formatting.
/// Why: a recruiter who edits the file keeps the look by picking a style; Word's navigation pane, accessibility
/// checker and an applicant tracking system read the built-in heading and list styles as structure.
/// </remarks>
internal static class DocxStyles
{
    /// <summary>The numbering instance the List Bullet style is bound to.</summary>
    public const int BulletNumbering = 1;

    public static Styles Build(DocxLook look, string language)
    {
        var designed = look.IsDesigned;
        var styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(new RunPropertiesBaseStyle
                {
                    RunFonts = Fonts(look.Sans),
                    Color = new Color { Val = look.Body },
                    FontSize = new FontSize { Val = Size(look.BodySize) },
                    FontSizeComplexScript = new FontSizeComplexScript { Val = Size(look.BodySize) },
                    Languages = language.Length > 0 ? new Languages { Val = language } : null,
                }),
                new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle
                {
                    SpacingBetweenLines = LineSpacing(before: 0, after: 80, line: designed ? 276 : 259),
                })));

        styles.Append(
            Paragraph(StyleIds.Normal, "Normal", basedOn: null, isDefault: true),
            Paragraph(
                StyleIds.Title,
                "Title",
                paragraph: new StyleParagraphProperties { SpacingBetweenLines = LineSpacing(0, 40, 240) },
                run: new StyleRunProperties
                {
                    Bold = new Bold(),
                    Color = new Color { Val = look.Heading },
                    Kern = new Kern { Val = 28 },
                    FontSize = new FontSize { Val = Size(look.NameSize) },
                    FontSizeComplexScript = new FontSizeComplexScript { Val = Size(look.NameSize) },
                }),
            Paragraph(
                StyleIds.Subtitle,
                "Subtitle",
                paragraph: new StyleParagraphProperties { SpacingBetweenLines = LineSpacing(0, 40, null) },
                run: new StyleRunProperties
                {
                    Color = new Color { Val = designed ? look.Accent : look.Heading },
                    FontSize = new FontSize { Val = Size(look.SubtitleSize) },
                    FontSizeComplexScript = new FontSizeComplexScript { Val = Size(look.SubtitleSize) },
                }),
            Paragraph(
                StyleIds.Tagline,
                "Tagline",
                paragraph: new StyleParagraphProperties { SpacingBetweenLines = LineSpacing(0, 60, null) },
                run: new StyleRunProperties { Color = new Color { Val = look.Muted } }),
            Paragraph(
                StyleIds.FocusLine,
                "Focus",
                paragraph: new StyleParagraphProperties { SpacingBetweenLines = LineSpacing(0, 80, null) },
                run: new StyleRunProperties { Color = new Color { Val = look.Heading } }),
            Paragraph(
                StyleIds.Contact,
                "Contact",
                paragraph: new StyleParagraphProperties { SpacingBetweenLines = LineSpacing(0, designed ? 20 : 0, null) },
                run: designed ? Small(look, look.Muted, look.Mono) : null),
            Heading(StyleIds.Heading1, "heading 1", 0, new StyleParagraphProperties
            {
                KeepNext = new KeepNext(),
                KeepLines = new KeepLines(),
                ParagraphBorders = new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = designed ? 4U : 6U, Space = 3U, Color = look.Rule }),
                SpacingBetweenLines = LineSpacing(designed ? 360 : 280, designed ? 120 : 100, null),
            }, new StyleRunProperties
            {
                Bold = new Bold(),
                BoldComplexScript = new BoldComplexScript(),
                Caps = designed ? new Caps() : null,
                Color = new Color { Val = look.Heading },
                Spacing = designed ? new Spacing { Val = 24 } : null,
                FontSize = new FontSize { Val = Size(look.Heading1Size) },
                FontSizeComplexScript = new FontSizeComplexScript { Val = Size(look.Heading1Size) },
            }),
            Heading(StyleIds.Heading2, "heading 2", 1, new StyleParagraphProperties
            {
                KeepNext = new KeepNext(),
                KeepLines = new KeepLines(),
                Tabs = RightTab(look),
                SpacingBetweenLines = LineSpacing(designed ? 220 : 200, 20, null),
            }, new StyleRunProperties
            {
                Bold = new Bold(),
                BoldComplexScript = new BoldComplexScript(),
                Color = new Color { Val = look.Heading },
                FontSize = new FontSize { Val = Size(look.Heading2Size) },
                FontSizeComplexScript = new FontSizeComplexScript { Val = Size(look.Heading2Size) },
            }),
            Heading(StyleIds.Heading3, "heading 3", 2, new StyleParagraphProperties
            {
                KeepNext = new KeepNext(),
                KeepLines = new KeepLines(),
                Tabs = RightTab(look),
                SpacingBetweenLines = LineSpacing(designed ? 140 : 160, 20, null),
            }, new StyleRunProperties
            {
                Bold = new Bold(),
                BoldComplexScript = new BoldComplexScript(),
                Color = new Color { Val = look.Heading },
                FontSize = new FontSize { Val = Size(look.Heading3Size) },
                FontSizeComplexScript = new FontSizeComplexScript { Val = Size(look.Heading3Size) },
            }),
            Paragraph(
                StyleIds.ItemMeta,
                "Item details",
                paragraph: new StyleParagraphProperties { KeepNext = new KeepNext(), SpacingBetweenLines = LineSpacing(0, 60, null) },
                run: new StyleRunProperties { Color = new Color { Val = designed ? look.Body : look.Muted } }),
            Paragraph(
                StyleIds.GroupLabel,
                "Group label",
                paragraph: new StyleParagraphProperties { KeepNext = new KeepNext(), KeepLines = new KeepLines(), SpacingBetweenLines = LineSpacing(160, designed ? 0 : 20, null) },
                run: designed
                    ? new StyleRunProperties
                    {
                        Bold = new Bold(),
                        Caps = new Caps(),
                        Color = new Color { Val = look.Muted },
                        Spacing = new Spacing { Val = 20 },
                        FontSize = new FontSize { Val = Size(look.SmallSize - 1) },
                        FontSizeComplexScript = new FontSizeComplexScript { Val = Size(look.SmallSize - 1) },
                    }
                    : new StyleRunProperties { Bold = new Bold(), Color = new Color { Val = look.Heading } }),
            Paragraph(
                StyleIds.ListBullet,
                "List Bullet",
                paragraph: new StyleParagraphProperties
                {
                    NumberingProperties = new NumberingProperties(new NumberingId { Val = BulletNumbering }),
                    Tabs = RightTab(look),
                    SpacingBetweenLines = LineSpacing(0, 40, null),
                    Indentation = new Indentation { Left = Twips(look.BulletIndent), Hanging = Twips(look.BulletIndent) },
                }),
            Paragraph(
                StyleIds.ListContinue,
                "List Continue",
                paragraph: new StyleParagraphProperties
                {
                    SpacingBetweenLines = LineSpacing(0, 40, null),
                    Indentation = new Indentation { Left = Twips(look.BulletIndent) },
                },
                run: new StyleRunProperties { Color = new Color { Val = look.Muted } }),
            Paragraph(
                StyleIds.Technologies,
                "Technologies",
                paragraph: new StyleParagraphProperties { SpacingBetweenLines = LineSpacing(designed ? 60 : 40, designed ? 60 : 80, null) },
                run: designed ? Small(look, look.Muted, look.Mono, noProof: true) : new StyleRunProperties { NoProof = new NoProof() }),
            Paragraph(
                StyleIds.SkillGroup,
                "Skill group",
                paragraph: new StyleParagraphProperties { KeepNext = new KeepNext(), KeepLines = new KeepLines(), SpacingBetweenLines = LineSpacing(designed ? 180 : 120, designed ? 40 : 20, null) },
                run: new StyleRunProperties { Bold = new Bold(), Color = new Color { Val = look.Heading } }),
            Paragraph(
                StyleIds.Skill,
                "Skill",
                paragraph: new StyleParagraphProperties { SpacingBetweenLines = LineSpacing(0, designed ? 30 : 20, designed ? 252 : null) },
                run: designed ? Small(look, look.Body, look.Sans, size: look.BodySize - 1) : null),
            new Style(
                new StyleName { Val = "Normal Table" },
                new UIPriority { Val = 99 },
                new SemiHidden(),
                new UnhideWhenUsed(),
                new StyleTableProperties(
                    new TableIndentation { Width = 0, Type = TableWidthUnitValues.Dxa },
                    new TableCellMarginDefault(
                        new TopMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
                        new TableCellLeftMargin { Width = 108, Type = TableWidthValues.Dxa },
                        new BottomMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
                        new TableCellRightMargin { Width = 108, Type = TableWidthValues.Dxa })))
            { Type = StyleValues.Table, Default = true, StyleId = StyleIds.TableNormal },
            new Style(
                new StyleName { Val = "Default Paragraph Font" },
                new UIPriority { Val = 1 },
                new SemiHidden(),
                new UnhideWhenUsed())
            { Type = StyleValues.Character, Default = true, StyleId = StyleIds.DefaultParagraphFont },
            Character(StyleIds.Hyperlink, "Hyperlink", new StyleRunProperties
            {
                NoProof = new NoProof(),
                Color = new Color { Val = look.Accent },
                Underline = designed ? null : new Underline { Val = UnderlineValues.Single },
            }),
            Character(StyleIds.Strong, "Strong", new StyleRunProperties
            {
                Bold = new Bold(),
                BoldComplexScript = new BoldComplexScript(),
                Color = designed ? new Color { Val = look.Heading } : null,
            }),
            Character(StyleIds.Muted, "Muted", new StyleRunProperties { Color = new Color { Val = look.Muted } }),
            Character(StyleIds.Date, "Date", Aside(look)),
            Character(StyleIds.Note, "Note", Aside(look)));

        if (designed)
        {
            styles.Append(
                Character(StyleIds.MeterFilled, "Level meter", Meter(look, look.Accent)),
                Character(StyleIds.MeterEmpty, "Level meter empty", Meter(look, look.Subtle)));
        }

        return styles;
    }

    /// <summary>The single bullet list every List Bullet paragraph belongs to.</summary>
    public static Numbering Bullets(DocxLook look) => new(
        new AbstractNum(
            new MultiLevelType { Val = MultiLevelValues.SingleLevel },
            new Level
            {
                LevelIndex = 0,
                StartNumberingValue = new StartNumberingValue { Val = 1 },
                NumberingFormat = new NumberingFormat { Val = NumberFormatValues.Bullet },
                ParagraphStyleIdInLevel = new ParagraphStyleIdInLevel { Val = StyleIds.ListBullet },
                LevelText = new LevelText { Val = look.Bullet },
                LevelJustification = new LevelJustification { Val = LevelJustificationValues.Left },
                PreviousParagraphProperties = new PreviousParagraphProperties(
                    new Indentation { Left = Twips(look.BulletIndent), Hanging = Twips(look.BulletIndent) }),
                NumberingSymbolRunProperties = new NumberingSymbolRunProperties(
                    Fonts(look.BulletFont),
                    new Color { Val = look.Accent }),
            })
        { AbstractNumberId = 0 },
        new NumberingInstance(new AbstractNumId { Val = 0 }) { NumberID = BulletNumbering });

    private static Style Paragraph(string id, string name, string? basedOn = StyleIds.Normal, bool isDefault = false, StyleParagraphProperties? paragraph = null, StyleRunProperties? run = null)
    {
        var style = new Style { Type = StyleValues.Paragraph, StyleId = id, Default = isDefault ? true : null };
        style.Append(new StyleName { Val = name });
        if (basedOn is not null)
        {
            style.Append(new BasedOn { Val = basedOn }, new NextParagraphStyle { Val = id == StyleIds.ListBullet ? id : StyleIds.Normal });
        }

        style.Append(new UIPriority { Val = isDefault ? 0 : 9 }, new PrimaryStyle());
        if (paragraph is not null)
        {
            style.Append(paragraph);
        }

        if (run is not null)
        {
            style.Append(run);
        }

        return style;
    }

    private static Style Heading(string id, string name, int level, StyleParagraphProperties paragraph, StyleRunProperties run)
    {
        paragraph.OutlineLevel = new OutlineLevel { Val = level };
        return Paragraph(id, name, paragraph: paragraph, run: run);
    }

    private static Style Character(string id, string name, StyleRunProperties run) => new(
        new StyleName { Val = name },
        new BasedOn { Val = StyleIds.DefaultParagraphFont },
        new UIPriority { Val = 9 },
        new PrimaryStyle(),
        run)
    { Type = StyleValues.Character, StyleId = id };

    private static StyleRunProperties Small(DocxLook look, string color, string font, bool noProof = false, int? size = null) => new()
    {
        RunFonts = Fonts(font),
        NoProof = noProof ? new NoProof() : null,
        Color = new Color { Val = color },
        FontSize = new FontSize { Val = Size(size ?? look.SmallSize) },
        FontSizeComplexScript = new FontSizeComplexScript { Val = Size(size ?? look.SmallSize) },
    };

    // Dates and notes beside a heading: regular weight, muted, a step smaller in the designed layout.
    private static StyleRunProperties Aside(DocxLook look) => new()
    {
        Bold = new Bold { Val = false },
        BoldComplexScript = new BoldComplexScript { Val = false },
        Color = new Color { Val = look.Muted },
        FontSize = look.IsDesigned ? new FontSize { Val = Size(look.SmallSize + 1) } : null,
        FontSizeComplexScript = look.IsDesigned ? new FontSizeComplexScript { Val = Size(look.SmallSize + 1) } : null,
    };

    private static StyleRunProperties Meter(DocxLook look, string color) => new()
    {
        RunFonts = Fonts(look.MeterFont),
        NoProof = new NoProof(),
        Color = new Color { Val = color },
        Spacing = new Spacing { Val = 8 },
        FontSize = new FontSize { Val = Size(look.BodySize - 1) },
        FontSizeComplexScript = new FontSizeComplexScript { Val = Size(look.BodySize - 1) },
    };

    private static Tabs RightTab(DocxLook look) => new(new TabStop { Val = TabStopValues.Right, Position = look.TextWidth });

    private static RunFonts Fonts(string font) => new() { Ascii = font, HighAnsi = font, EastAsia = font, ComplexScript = font };

    private static SpacingBetweenLines LineSpacing(int before, int after, int? line)
    {
        var spacing = new SpacingBetweenLines { Before = Twips(before), After = Twips(after) };
        if (line is { } value)
        {
            spacing.Line = Twips(value);
            spacing.LineRule = LineSpacingRuleValues.Auto;
        }

        return spacing;
    }

    private static string Twips(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Size(int halfPoints) => Twips(halfPoints);
}
