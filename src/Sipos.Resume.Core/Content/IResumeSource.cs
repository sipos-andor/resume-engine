namespace Sipos.Resume.Core.Content;

/// <summary>Where a CV's content comes from: a folder at build time, an upload or a URL in the Studio.</summary>
public interface IResumeSource
{
    /// <summary>Reads every content file: the <c>resume.{language}.json</c> files and <c>site.json</c>.</summary>
    /// <param name="cancellationToken">Stops the reading.</param>
    ValueTask<IReadOnlyList<ContentFile>> ReadAsync(CancellationToken cancellationToken);
}
