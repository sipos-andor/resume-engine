using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Sipos.Resume.Core.Search;

/// <summary>
/// Folds text for matching: lowercase, without diacritics, split into tokens that keep technology names whole
/// (<c>c#</c>, <c>.net</c>, <c>node.js</c>, <c>c++</c>).
/// </summary>
/// <remarks>
/// Decision: the same rules run in C# at build time and in the browser's JavaScript (<c>cv-text.js</c> of the theme),
/// and shared test vectors check that both give the same tokens.
/// Why: the search index and the job ad vocabulary are built here, the queries are folded there; a reader who types
/// <c>kodbazis</c> must find <c>kódbázis</c>, and <c>.NET</c> must not become <c>net</c>.
/// </remarks>
public static partial class TextNormalizer
{
    /// <summary>Lowercases and removes diacritics, keeping the text's other characters.</summary>
    /// <param name="text">The text, such as <c>Kódbázis, Đurđevo</c>.</param>
    public static string Fold(string text)
    {
        var lower = Transliterate(text.ToLowerInvariant()).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Splits text into folded tokens, keeping <c>#</c>, <c>+</c> and inner dots of technology names.</summary>
    /// <param name="text">The text.</param>
    public static IReadOnlyList<string> Tokens(string text) =>
        [.. Token().Matches(Fold(text)).Select(match => match.Value.TrimEnd('.')).Where(token => token.Length > 0 && token != ".")];

    // Letters that do not decompose into a base letter and a mark, and the two that lowercase differently in .NET and
    // JavaScript: İ, which ToLowerInvariant keeps and toLowerCase turns into i and a dot, and the final ς, which
    // toLowerCase writes at a word's end and ToLowerInvariant never; JavaScript maps the same ones.
    private static string Transliterate(string text) => text
        .Replace("İ", "i", StringComparison.Ordinal)
        .Replace("ς", "σ", StringComparison.Ordinal)
        .Replace("đ", "d", StringComparison.Ordinal)
        .Replace("ł", "l", StringComparison.Ordinal)
        .Replace("ø", "o", StringComparison.Ordinal)
        .Replace("ß", "ss", StringComparison.Ordinal)
        .Replace("æ", "ae", StringComparison.Ordinal)
        .Replace("œ", "oe", StringComparison.Ordinal);

    [GeneratedRegex(@"[\p{L}\p{N}#+.]+", RegexOptions.CultureInvariant)]
    private static partial Regex Token();
}
