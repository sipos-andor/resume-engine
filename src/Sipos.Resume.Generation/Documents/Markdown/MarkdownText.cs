using System.Text;
using System.Text.RegularExpressions;

namespace Sipos.Resume.Generation.Documents.Markdown;

/// <summary>Writes text and links into Markdown so they read as written.</summary>
internal static partial class MarkdownText
{
    /// <summary>Escapes the characters CommonMark would read as markup inside a line, such as <c>*</c> in <c>C*</c>.</summary>
    /// <param name="text">Plain text.</param>
    public static string Escape(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var escaped = new StringBuilder(text.Length + 8);
        foreach (var character in text.ReplaceLineEndings(" "))
        {
            if (character is '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '>' or '|' or '&')
            {
                escaped.Append('\\');
            }

            escaped.Append(character);
        }

        return escaped.ToString();
    }

    /// <summary>
    /// Escapes text that starts a block, such as a paragraph or a list item's content, so that a leading <c>#</c>,
    /// <c>-</c>, <c>+</c>, <c>1.</c> or <c>1)</c> stays text instead of becoming a heading or a list.
    /// </summary>
    /// <param name="text">Plain text.</param>
    public static string EscapeBlock(string text)
    {
        var escaped = Escape(text);
        if (escaped.Length > 0 && escaped[0] is '#' or '-' or '+')
        {
            return "\\" + escaped;
        }

        // Decision: escape a number's dot or parenthesis only where it starts a block and a space follows.
        // Why: CommonMark reads "2023. March" there as a list starting at 2023, but a date inside a line is plain text.
        return OrderedListMarker().Match(escaped) is { Success: true } marker
            ? escaped.Insert(marker.Length - 1, "\\")
            : escaped;
    }

    /// <summary>Writes a URL as a link whose text is the URL without its scheme, such as <c>[github.com/ann](https://github.com/ann)</c>.</summary>
    /// <param name="url">An absolute URL.</param>
    public static string Link(string url) => $"[{Escape(Shorten(url))}]({Destination(url)})";

    /// <summary>
    /// Writes a URL as a link's destination: a space, a parenthesis or an angle bracket, which would end or break the
    /// link, is percent-encoded, as a browser sends it anyway.
    /// </summary>
    /// <param name="url">An absolute URL.</param>
    public static string Destination(string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        return url.Replace(" ", "%20", StringComparison.Ordinal).Replace("(", "%28", StringComparison.Ordinal).Replace(")", "%29", StringComparison.Ordinal)
            .Replace("<", "%3C", StringComparison.Ordinal).Replace(">", "%3E", StringComparison.Ordinal);
    }

    /// <summary>Returns a URL without its scheme and without the slash after a bare host, as people write it.</summary>
    /// <param name="url">An absolute URL.</param>
    public static string Shorten(string url)
    {
        var text = url.StartsWith("https://", StringComparison.Ordinal) ? url[8..] : url.StartsWith("http://", StringComparison.Ordinal) ? url[7..] : url;
        return text.EndsWith('/') && text.IndexOf('/', StringComparison.Ordinal) == text.Length - 1 ? text[..^1] : text;
    }

    [GeneratedRegex(@"^[0-9]{1,9}[.)](?=[ \t]|$)", RegexOptions.CultureInvariant)]
    private static partial Regex OrderedListMarker();
}
