using System.Diagnostics;

namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>A directory link; Windows junctions do not require symbolic-link privileges.</summary>
internal static class DirectoryLink
{
    public static void Create(string path, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(path, target);
            return;
        }

        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[] { "/c", "mklink", "/J", path, target })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, process.StandardError.ReadToEnd());
    }
}
