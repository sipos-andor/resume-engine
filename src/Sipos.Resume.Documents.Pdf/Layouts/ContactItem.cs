using Sipos.Resume.Core.Artifacts;

namespace Sipos.Resume.Documents.Pdf.Layouts;

/// <summary>What a contact item is, which decides how each layout writes it.</summary>
internal enum ContactKind
{
    /// <summary>The phone number.</summary>
    Phone,

    /// <summary>The e-mail address, drawn as an image; the item's text is the image's alternative text, never the address.</summary>
    Email,

    /// <summary>The online CV.</summary>
    Page,

    /// <summary>Where the person works from.</summary>
    Location,

    /// <summary>The way to get in touch, such as a contact form.</summary>
    Contact,

    /// <summary>A profile on another site.</summary>
    Profile,

    /// <summary>The availability indicator.</summary>
    Availability,
}

/// <summary>One item of the contact line.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Text">The text the designed layout shows.</param>
/// <param name="Label">The label the ATS layout puts in front, such as <c>Phone</c>, or <see langword="null"/>.</param>
/// <param name="Url">The link, or <see langword="null"/> for plain text.</param>
internal sealed record ContactItem(ContactKind Kind, string Text, string? Label, string? Url)
{
    /// <summary>
    /// Lists the contact items of a document in their order: phone, e-mail image, online CV, location, contact,
    /// profiles, availability.
    /// </summary>
    /// <param name="context">The document's context, whose page address is the online CV.</param>
    /// <param name="outline">The document's outline, for the labels.</param>
    public static IReadOnlyList<ContactItem> Of(DocumentContext context, DocumentOutline outline)
    {
        var person = context.Document.Person;
        var labels = outline.Labels;
        var items = new List<ContactItem>();
        if (person.Phone is { } phone)
        {
            items.Add(new ContactItem(ContactKind.Phone, phone, labels.Phone, $"tel:{person.PhoneDial}"));
        }

        if (!string.IsNullOrWhiteSpace(context.ContactEmail))
        {
            items.Add(new ContactItem(ContactKind.Email, labels.EmailImage, null, null));
        }

        items.Add(new ContactItem(ContactKind.Page, WithoutScheme(context.PageUrl), labels.Web, context.PageUrl.AbsoluteUri));
        if (person.Location is { } location)
        {
            items.Add(new ContactItem(ContactKind.Location, location, labels.Location, null));
        }

        if (person.Contact is { } contact)
        {
            items.Add(new ContactItem(ContactKind.Contact, contact.Label, labels.Contact, contact.Url));
        }

        items.AddRange(person.Profiles.Select(profile => new ContactItem(ContactKind.Profile, WithoutScheme(profile.Url), profile.Network, profile.Url)));
        if (person.Availability is { } availability)
        {
            items.Add(new ContactItem(ContactKind.Availability, availability.Label, null, availability.Url));
        }

        return items;
    }

    /// <summary>Writes an address the way people read it: <c>github.com/ann</c> for <c>https://github.com/ann/</c>.</summary>
    /// <param name="url">The absolute address.</param>
    public static string WithoutScheme(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? WithoutScheme(uri) : url;

    /// <inheritdoc cref="WithoutScheme(string)"/>
    public static string WithoutScheme(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        var port = url.IsDefaultPort ? "" : $":{url.Port}";
        return (url.Host + port + url.PathAndQuery).TrimEnd('/');
    }
}
