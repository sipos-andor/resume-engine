using System.Text;

namespace Sipos.Resume.Generation.Documents.Markdown;

/// <summary>Writes text and links into Markdown so they read as written.</summary>
internal static class MarkdownText
{
    /// <summary>Escapes the characters CommonMark would read as markup, such as <c>*</c> in <c>C*</c> or <c>#</c> in <c>C#</c> at a line's start.</summary>
    /// <param name="text">Plain text.</param>
    public static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length + 8);
        foreach (var character in text)
        {
            if (character is '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '>' or '|')
            {
                escaped.Append('\\');
            }

            escaped.Append(character == '\n' ? ' ' : character);
        }

        // A line that starts with '#', '-', '+' or a number and '.' would become a heading or a list item.
        if (escaped.Length > 0 && escaped[0] is '#' or '-' or '+')
        {
            escaped.Insert(0, '\\');
        }

        return escaped.ToString();
    }

    /// <summary>Writes a URL as a link whose text is the URL without its scheme, such as <c>[github.com/ann](https://github.com/ann)</c>.</summary>
    /// <param name="url">An absolute URL.</param>
    public static string Link(string url) => $"[{Escape(Shorten(url))}]({url})";

    /// <summary>Returns a URL without its scheme and without the slash after a bare host, as people write it.</summary>
    /// <param name="url">An absolute URL.</param>
    public static string Shorten(string url)
    {
        var text = url.StartsWith("https://", StringComparison.Ordinal) ? url[8..] : url.StartsWith("http://", StringComparison.Ordinal) ? url[7..] : url;
        return text.EndsWith('/') && text.IndexOf('/', StringComparison.Ordinal) == text.Length - 1 ? text[..^1] : text;
    }
}
