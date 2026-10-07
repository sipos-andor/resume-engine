using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Sipos.Resume.Documents.Pdf.Layouts;

/// <summary>The heading levels of both layouts, which never skip a level, as PDF/UA asks.</summary>
internal enum HeadingLevel
{
    /// <summary>The person's name.</summary>
    Name = 1,

    /// <summary>A section, such as Experience.</summary>
    Section = 2,

    /// <summary>An entry of a section: a position, a project of no position, a skill group.</summary>
    Entry = 3,

    /// <summary>A group inside an entry: the client projects of a position.</summary>
    Group = 4,

    /// <summary>An entry of a group: one client project.</summary>
    NestedEntry = 5,
}

/// <summary>Tags a container as a heading.</summary>
internal static class HeadingTags
{
    /// <summary>Tags the container as a heading of a level.</summary>
    /// <param name="container">The container.</param>
    /// <param name="level">The level.</param>
    public static IContainer Heading(this IContainer container, HeadingLevel level) => level switch
    {
        HeadingLevel.Name => container.SemanticHeading1(),
        HeadingLevel.Section => container.SemanticHeading2(),
        HeadingLevel.Entry => container.SemanticHeading3(),
        HeadingLevel.Group => container.SemanticHeading4(),
        _ => container.SemanticHeading5(),
    };
}
