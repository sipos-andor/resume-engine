using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Documents.Pdf.Layouts;

namespace Sipos.Resume.Documents.Pdf.Tests.Layouts;

public class ContactItemOf
{
    [Fact]
    public void ListsPhoneEmailOnlineCvLocationContactProfilesAndAvailabilityGivenFullBasics()
    {
        var context = PdfSamples.Context(SampleDocuments.English(), DocumentVariant.Designed, PdfSamples.Address);

        var items = ContactItem.Of(context, DocumentOutline.Of(context));

        items.Select(item => item.Kind).ShouldBe(
        [
            ContactKind.Phone, ContactKind.Email, ContactKind.Page, ContactKind.Location, ContactKind.Contact, ContactKind.Profile, ContactKind.Availability,
        ]);
        items.Select(item => item.Text).ShouldBe(["+36 30 123 4567", "E-mail address", "cv.example.com", "Remote from Europe", "Get in touch", "github.com/ann", "Available now"]);
    }

    [Fact]
    public void LeavesOutEmailGivenNoAddress()
    {
        var context = PdfSamples.Context(SampleDocuments.English(), DocumentVariant.Ats);

        ContactItem.Of(context, DocumentOutline.Of(context)).ShouldNotContain(item => item.Kind == ContactKind.Email);
    }

    // The item stands for the image; no field of it may hold the address.
    [Fact]
    public void NamesImageNotAddressGivenAddress()
    {
        var context = PdfSamples.Context(SampleDocuments.Hungarian(), DocumentVariant.Designed, PdfSamples.Address);

        var email = ContactItem.Of(context, DocumentOutline.Of(context)).Single(item => item.Kind == ContactKind.Email);

        email.ToString().ShouldNotContain("privacy.probe");
        email.Text.ShouldBe(DocumentOutline.Of(context).Labels.EmailImage);
    }

    [Theory]
    [InlineData("https://github.com/ann/", "github.com/ann")]
    [InlineData("https://www.linkedin.com/in/ann", "www.linkedin.com/in/ann")]
    [InlineData("https://cv.example.com:8443/hu/", "cv.example.com:8443/hu")]
    [InlineData("not a url", "not a url")]
    public void WritesAddressWithoutSchemeAndTrailingSlashGivenUrl(string url, string expected) =>
        ContactItem.WithoutScheme(url).ShouldBe(expected);
}
