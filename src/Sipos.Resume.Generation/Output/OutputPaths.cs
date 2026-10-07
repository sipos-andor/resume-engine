namespace Sipos.Resume.Generation.Output;

/// <summary>Rejects output paths that traverse symbolic links or junctions.</summary>
internal static class OutputPaths
{
    /// <summary>Finds a reparse point in an existing path component, including the target itself.</summary>
    public static string? LinkIn(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    return current;
                }
            }
            catch (FileNotFoundException)
            {
                // A new output can have several missing parents; check the existing ancestors too.
            }
            catch (DirectoryNotFoundException)
            {
                // Continue to the nearest existing ancestor.
            }
        }

        return null;
    }

    /// <summary>Checks before creating, deleting or writing output.</summary>
    public static void Check(string path)
    {
        if (LinkIn(path) is { } link)
        {
            throw new IOException($"Output path traverses a symbolic link or junction ({link}).");
        }
    }
}
