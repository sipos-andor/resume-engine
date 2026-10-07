using Sipos.Resume.Core.Content;

namespace Sipos.Resume.Generation.Content;

/// <summary>Reads a CV's content files from a folder: <c>site.json</c> and every <c>resume.*.json</c>.</summary>
/// <param name="folder">The content folder.</param>
public sealed class FolderSource(string folder) : IResumeSource
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ContentFile>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"The content folder '{folder}' does not exist.");
        }

        var files = new List<ContentFile>();
        foreach (var path in Directory.EnumerateFiles(folder, "*.json").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(path);
            if (name == ContentLoader.SiteFileName || name.StartsWith("resume.", StringComparison.Ordinal))
            {
                files.Add(new ContentFile(name, await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)));
            }
        }

        return files;
    }
}
