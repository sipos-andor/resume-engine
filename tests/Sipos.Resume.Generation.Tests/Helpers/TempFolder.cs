namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>A folder that is deleted when the test ends.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder() => Directory.CreateDirectory(Path);

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "resume-engine-tests", Guid.NewGuid().ToString("N"));

    public string Write(string relativePath, string content)
    {
        var file = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
        return file;
    }

    public string Read(string relativePath) => File.ReadAllText(System.IO.Path.Combine(Path, relativePath));

    public bool Exists(string relativePath) => File.Exists(System.IO.Path.Combine(Path, relativePath));

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
