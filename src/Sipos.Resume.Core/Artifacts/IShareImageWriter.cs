using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Artifacts;

/// <summary>Draws the image a CV's page is shared with: 1200 by 630 pixels, PNG.</summary>
public interface IShareImageWriter
{
    /// <summary>Draws the share image of a CV in one language.</summary>
    /// <param name="document">The CV.</param>
    /// <param name="theme">The colours and fonts.</param>
    /// <param name="site">The site's address, printed on the image as its host name.</param>
    /// <returns>The PNG's bytes.</returns>
    byte[] Write(ResumeDocument document, DocumentTheme theme, Uri site);
}
