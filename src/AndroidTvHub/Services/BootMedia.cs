using System.Diagnostics;

namespace AndroidTvHub.Services;

internal static class BootMedia
{
    public static bool IsExtracted => File.Exists(HubPaths.Kernel) && File.Exists(HubPaths.Initrd);

    public static void ExtractFromIso(string isoPath, IProgress<string>? log = null)
    {
        if (IsExtracted)
        {
            return;
        }

        if (!File.Exists(isoPath))
        {
            throw new FileNotFoundException("Guest ISO is missing.", isoPath);
        }

        HubPaths.EnsureLayout();
        log?.Report("Extracting kernel and initrd from the Guest ISO…");

        var script = string.Join(Environment.NewLine, new[]
        {
            "$ErrorActionPreference = 'Stop'",
            $"$iso = '{isoPath.Replace("'", "''")}'",
            $"$kernel = '{HubPaths.Kernel.Replace("'", "''")}'",
            $"$initrd = '{HubPaths.Initrd.Replace("'", "''")}'",
            "$img = Mount-DiskImage -ImagePath $iso -PassThru",
            "try {",
            "  $letter = ($img | Get-Volume).DriveLetter",
            "  Copy-Item \"${letter}:\\KERNEL\" $kernel -Force",
            "  Copy-Item \"${letter}:\\INITRD.IMG\" $initrd -Force",
            "} finally {",
            "  Dismount-DiskImage -ImagePath $iso | Out-Null",
            "}"
        });

        var start = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -ExecutionPolicy Bypass -Command -",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(start) ?? throw new InvalidOperationException("PowerShell failed to start.");
        p.StandardInput.Write(script);
        p.StandardInput.Close();
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit(120000);
        if (p.ExitCode != 0 || !IsExtracted)
        {
            throw new InvalidOperationException("Could not extract kernel/initrd from the ISO. " + err);
        }

        log?.Report("Boot files ready.");
    }
}
