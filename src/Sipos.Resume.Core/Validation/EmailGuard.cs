using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sipos.Resume.Core.Validation;

/// <summary>Finds e-mail addresses and <c>mailto:</c> links anywhere in a content file's text values.</summary>
/// <remarks>
/// Decision: content may hold no e-mail address at all, in any field; the build fails on one.
/// Why: everything in a content file is published (web pages, Markdown, JSON Resume, llms.txt), and an address on a
/// public page is harvested. A contact form or a phone number stands in for it; an address meant for documents only
/// reaches them at build time from a secret, never from the content.
/// Considered: refusing only <c>basics.email</c>, which misses an address written into a summary.
/// </remarks>
public static partial class EmailGuard
{
    /// <summary>
    /// Whether a text contains an e-mail address or a <c>mailto:</c> link, also written with a character reference
    /// (<c>&amp;#64;</c>, <c>&amp;commat;</c>) or percent-encoded (<c>%40</c>), as a Markdown or HTML reader would
    /// show it. A Fediverse handle such as <c>@ann@mastodon.social</c> is no address.
    /// </summary>
    /// <param name="text">The text to check.</param>
    public static bool ContainsAddress(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var shown = Shown(text);
        return Address().IsMatch(shown) || shown.Contains("mailto:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Replaces every address and <c>mailto:</c> in a text, such as an issue that quotes a value, before it is logged.</summary>
    /// <param name="text">A text that may quote content.</param>
    public static string Redact(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Address().Replace(MailtoLink().Replace(Shown(text), "[e-mail address]"), "[e-mail address]");
    }

    /// <summary>Returns an issue for every string value of a JSON document that contains an address.</summary>
    /// <param name="source">The file's name, for the issues.</param>
    /// <param name="root">The document's root element.</param>
    public static IEnumerable<ValidationIssue> Check(string source, JsonElement root) => Walk(source, root, "");

    private static IEnumerable<ValidationIssue> Walk(string source, JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var pointer = $"{path}/{Escape(property.Name)}";
                    if (ContainsAddress(property.Name))
                    {
                        yield return new ValidationIssue(source, pointer, "A property name contains an e-mail address.");
                    }

                    foreach (var issue in Walk(source, property.Value, pointer))
                    {
                        yield return issue;
                    }
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var issue in Walk(source, item, $"{path}/{index++}"))
                    {
                        yield return issue;
                    }
                }

                break;
            case JsonValueKind.String when ContainsAddress(element.GetString()!):
                yield return new ValidationIssue(source, path, "Contains an e-mail address or a mailto: link; content is published, so leave it out and link a contact page instead.");
                break;
        }
    }

    // The text as a reader sees it: character references decoded (HtmlDecode knows HTML 4's, &commat; is HTML5's) and
    // percent-encoding undone, again and again until nothing changes, so ann&amp;#64; or ann%2540 comes out as ann@.
    // Decoding only shortens the text, so the loop ends; the bound keeps a pathological text cheap.
    private static string Shown(string text)
    {
        var shown = text;
        for (var pass = 0; pass < 8; pass++)
        {
            var next = Uri.UnescapeDataString(WebUtility.HtmlDecode(shown).Replace("&commat;", "@", StringComparison.OrdinalIgnoreCase));
            if (next == shown)
            {
                break;
            }

            shown = next;
        }

        return shown;
    }

    // JSON pointer escaping (RFC 6901): ~ becomes ~0 and / becomes ~1.
    private static string Escape(string name) => name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    // A mailto: link up to the quote, bracket or space that ends it, so "a mailto: link" in a message stays.
    [GeneratedRegex(@"mailto:[^\s'""<>()\[\]]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex MailtoLink();

    // Decision: letters, marks and digits of any script on both sides of the @, and a top-level domain in letters or in
    // punycode (xn--…), so an internationalized address such as one with a Greek or Cyrillic local part or domain is
    // caught too; a local part right after an @ is a Fediverse handle's user, not an address's.
    // Why: the guard is the privacy promise; an address it does not recognize is published as text. A handle is public
    // by design, and the build's own address is checked word for word besides.
    [GeneratedRegex(@"(?<![@\p{L}\p{M}\p{N}._%+\-])[\p{L}\p{M}\p{N}._%+\-]+@[\p{L}\p{M}\p{N}\-]+(\.[\p{L}\p{M}\p{N}\-]+)*\.([\p{L}\p{M}]{2,}|xn--[a-z0-9\-]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex Address();
}
