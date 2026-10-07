namespace Sipos.Resume.Generation.Output;

/// <summary>Writes the site into a folder, which is then uploaded as is.</summary>
/// <param name="root">The folder; site paths are written below it.</param>
public sealed class FolderSink(string root) : IArtifactSink
{
    private readonly string _root = Path.GetFullPath(root);
    private readonly List<string> _written = [];
    private readonly HashSet<string> _claimed = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public IReadOnlyList<string> Written
    {
        get
        {
            lock (_gate)
            {
                return [.. _written];
            }
        }
    }

    /// <summary>The folder's full path.</summary>
    public string Root => _root;

    /// <inheritdoc />
    public async Task WriteAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        SitePaths.Check(path);
        // Decision: a path is claimed case-insensitively and only once.
        // Why: GitHub Pages serves from a case-sensitive store, but a contributor's disk may not be, and two outputs
        // for one path mean one of them silently replaced the other.
        lock (_gate)
        {
            if (!_claimed.Add(path))
            {
                throw new InvalidOperationException($"'{path}' was already written.");
            }

            _written.Add(path);
        }

        var file = Path.Combine(_root, path[1..].Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await File.WriteAllBytesAsync(file, content.ToArray(), cancellationToken).ConfigureAwait(false);
    }
}
