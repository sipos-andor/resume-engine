using System.Globalization;
using System.Text;
using Sipos.Resume.Core.Content;

namespace Sipos.Resume.Core.Mapping;

/// <summary>The identifiers of positions and projects: the file's <c>x-id</c>, or one made from the name and start date.</summary>
/// <remarks>
/// Decision: an item without <c>x-id</c> still gets an identifier, a slug of its name and start date.
/// Why: a plain JSON Resume has no identifiers, and anchors, the timeline and the focus views need one; organization
/// names and dates are the same in every language, so a position's made-up identifier is too. Projects with different
/// names across languages must supply the same explicit <c>x-id</c> in every language.
/// </remarks>
public static class Identifiers
{
    /// <summary>The identifier of a position.</summary>
    /// <param name="work">The position as the file has it.</param>
    public static string Of(JsonResumeWork work) => work.Id is { Length: > 0 } id ? id : Slug($"{work.Name} {work.StartDate}");

    /// <summary>The identifier of a project.</summary>
    /// <param name="project">The project as the file has it.</param>
    public static string Of(JsonResumeProject project) => project.Id is { Length: > 0 } id ? id : Slug($"{project.Name} {project.StartDate}");

    /// <summary>Whether a text is a valid identifier: lowercase ASCII letters, digits and hyphens, starting with a letter or digit.</summary>
    /// <param name="id">The text.</param>
    public static bool IsValid(string id) =>
        id.Length is > 0 and <= 64 && IsLowerOrDigit(id[0]) && id.All(c => IsLowerOrDigit(c) || c == '-');

    // Letters that do not decompose into a base letter and a mark.
    private static string Transliterate(string text) => text
        .Replace("đ", "d", StringComparison.Ordinal).Replace("Đ", "D", StringComparison.Ordinal)
        .Replace("ł", "l", StringComparison.Ordinal).Replace("Ł", "L", StringComparison.Ordinal)
        .Replace("ø", "o", StringComparison.Ordinal).Replace("Ø", "O", StringComparison.Ordinal)
        .Replace("ß", "ss", StringComparison.Ordinal).Replace("æ", "ae", StringComparison.Ordinal).Replace("Æ", "AE", StringComparison.Ordinal);

    private static bool IsLowerOrDigit(char c) => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c);

    /// <summary>Makes an identifier from a text: lowercase, without diacritics, words joined by hyphens.</summary>
    /// <param name="text">The text, such as <c>ALLWIN Informatika Kft. 2015-03</c>.</param>
    public static string Slug(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in Transliterate(text).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length > 64 ? slug[..64].TrimEnd('-') : slug;
    }
}
