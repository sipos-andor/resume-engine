using System.Text.RegularExpressions;

namespace Sipos.Resume.Core.Mapping;

/// <summary>
/// The element ids a CV's page uses for itself, which an item's identifier must not take: the sections, the generated
/// anchors of skill groups, studies, certificates and awards, and the prefixes and suffixes themes keep for their own.
/// </summary>
/// <remarks>
/// Decision: one reserved set in the core, checked against every identifier of the content.
/// Why: positions, projects and strengths are anchors of the same page as the sections and the engine's generated
/// anchors (<c>skills-1</c>, <c>education-2</c>); a clash gives the page two elements with one id, and a deep link or
/// a search hit then leads to the wrong one. A theme names its own elements within these rules.
/// </remarks>
public static partial class Anchors
{
    /// <summary>The section ids every theme may use.</summary>
    public static IReadOnlySet<string> Sections { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "main", "top", "tools", "profile", "strengths", "experience", "projects", "skills", "education", "certificates",
        "awards", "languages", "downloads", "timeline", "contact",
    };

    /// <summary>Whether an identifier is one the page uses for itself.</summary>
    /// <param name="id">An item's identifier.</param>
    public static bool IsReserved(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return Sections.Contains(id) || Generated().IsMatch(id) || id.StartsWith("cv-", StringComparison.Ordinal) || id.EndsWith("-title", StringComparison.Ordinal);
    }

    [GeneratedRegex("^(skills|education|certificate|award)-[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Generated();
}
