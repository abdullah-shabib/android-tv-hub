using System.Diagnostics;

namespace AndroidTvHub.Services;

internal readonly record struct WhpxStatus(bool SupportedInQemu, bool AcceleratorAccepted, string Detail);

internal static class WhpxProbe
{
    public static WhpxStatus Probe(string qemuSystem)
    {
        var help = Run(qemuSystem, "-accel help", 4000);
        var listsWhpx = help.StdOut.Contains("whpx", StringComparison.OrdinalIgnoreCase);
        if (!listsWhpx)
        {
            return new WhpxStatus(false, false, "This QEMU binary was not built with WHPX.");
        }

        var probe = Run(
            qemuSystem,
            "-accel whpx,kernel-irqchip=off -machine q35 -m 256 -display none -serial none -monitor none",
            3500,
            killIfStillRunning: true);

        if (probe.KilledBecauseStillRunning)
        {
            return new WhpxStatus(true, true, "WHPX accepted a short-lived QEMU process.");
        }

        var err = probe.StdErr;
        if (err.Contains("whpx", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("Hypervisor", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("WHPX", StringComparison.Ordinal))
        {
            return new WhpxStatus(true, false, err.Trim());
        }

        if (probe.ExitCode is 0)
        {
            return new WhpxStatus(true, true, "WHPX probe exited cleanly.");
        }

        return new WhpxStatus(true, false, string.IsNullOrWhiteSpace(err) ? $"QEMU exited {probe.ExitCode}." : err.Trim());
    }

    private readonly record struct RunResult(string StdOut, string StdErr, int? ExitCode, bool KilledBecauseStillRunning);

    private static RunResult Run(string fileName, string arguments, int timeoutMs, bool killIfStillRunning = false)
    {
        var start = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(start);
        if (p is null)
        {
            return new RunResult("", "failed to start QEMU", null, false);
        }

        var stdout = p.StandardOutput;
        var stderr = p.StandardError;
        if (!p.WaitForExit(timeoutMs))
        {
            if (killIfStillRunning)
            {
                try
                {
                    p.Kill(entireProcessTree: true);
                }
                catch
                {
                    // ignore
                }
            }

            return new RunResult(SafeRead(stdout), SafeRead(stderr), null, true);
        }

        return new RunResult(SafeRead(stdout), SafeRead(stderr), p.ExitCode, false);
    }

    private static string SafeRead(StreamReader reader)
    {
        try
        {
            return reader.ReadToEnd();
        }
        catch
        {
            return "";
        }
    }
}
