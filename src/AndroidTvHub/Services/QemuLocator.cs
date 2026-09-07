using System.Diagnostics;

namespace AndroidTvHub.Services;

internal sealed class QemuLocator
{
    public string? QemuSystem { get; private set; }
    public string? QemuImg { get; private set; }

    public bool TryFind(HubSettings settings)
    {
        foreach (var dir in CandidateDirectories(settings))
        {
            var system = Path.Combine(dir, "qemu-system-x86_64.exe");
            var img = Path.Combine(dir, "qemu-img.exe");
            if (File.Exists(system) && File.Exists(img))
            {
                QemuSystem = system;
                QemuImg = img;
                return true;
            }
        }

        QemuSystem = null;
        QemuImg = null;
        return false;
    }

    public static string? Version(string qemuSystem)
    {
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = qemuSystem,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(start);
            if (p is null)
            {
                return null;
            }

            var text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(4000);
            return text.Split('\n')[0].Trim();
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> CandidateDirectories(HubSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.QemuDirectory))
        {
            yield return settings.QemuDirectory;
        }

        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "qemu");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "qemu");
        yield return HubPaths.QemuImgDefault;
    }
}
