namespace Sipos.Resume.Generation.Output;

/// <summary>Where the build writes the site: a folder, or memory in tests.</summary>
public interface IArtifactSink
{
    /// <summary>The site paths written so far, such as <c>/hu/index.html</c>, in the order they were written.</summary>
    IReadOnlyList<string> Written { get; }

    /// <summary>Writes a file.</summary>
    /// <param name="path">The site path, starting with <c>/</c>.</param>
    /// <param name="content">The file's bytes.</param>
    /// <param name="cancellationToken">Stops the writing.</param>
    /// <exception cref="InvalidOperationException">The path was already written; two outputs must never claim one path.</exception>
    Task WriteAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);
}
