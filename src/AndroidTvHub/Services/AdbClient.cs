using System.Diagnostics;
using System.IO.Compression;

namespace AndroidTvHub.Services;

internal sealed class AdbClient
{
    private const string PlatformToolsZip = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";

    public async Task EnsureAdbAsync(IProgress<string> log, CancellationToken cancellationToken)
    {
        if (File.Exists(HubPaths.Adb))
        {
            return;
        }

        HubPaths.EnsureLayout();
        var zipPath = Path.Combine(HubPaths.Tools, "platform-tools.zip");
        log.Report("Downloading Android platform-tools (adb)…");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AndroidTvHub/0.1");
        using var response = await http.GetAsync(PlatformToolsZip, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using (var file = File.Create(zipPath))
        {
            await response.Content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        }

        var extract = Path.Combine(HubPaths.Tools, "platform-tools-extract");
        if (Directory.Exists(extract))
        {
            Directory.Delete(extract, recursive: true);
        }

        ZipFile.ExtractToDirectory(zipPath, extract);
        var adbDir = Directory.GetDirectories(extract, "platform-tools", SearchOption.AllDirectories).FirstOrDefault();
        if (adbDir is null)
        {
            throw new InvalidOperationException("platform-tools zip did not contain adb.");
        }

        if (Directory.Exists(HubPaths.PlatformTools))
        {
            Directory.Delete(HubPaths.PlatformTools, recursive: true);
        }

        Directory.Move(adbDir, HubPaths.PlatformTools);
        File.Delete(zipPath);
        try
        {
            Directory.Delete(extract, recursive: true);
        }
        catch
        {
            // ignore leftover extract dir
        }

        log.Report("adb ready.");
    }

    public async Task<string> ConnectAsync(int port, CancellationToken cancellationToken)
    {
        await RunAsync("start-server", cancellationToken).ConfigureAwait(false);
        return await RunAsync($"connect 127.0.0.1:{port}", cancellationToken).ConfigureAwait(false);
    }

    public Task<string> DevicesAsync(CancellationToken cancellationToken) =>
        RunAsync("devices", cancellationToken);

    public async Task<string> SideloadAsync(string apkPath, int port, CancellationToken cancellationToken)
    {
        if (!File.Exists(apkPath))
        {
            throw new FileNotFoundException("APK not found.", apkPath);
        }

        await ConnectAsync(port, cancellationToken).ConfigureAwait(false);
        return await RunAsync($"-s 127.0.0.1:{port} install -r \"{apkPath}\"", cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> RunAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!File.Exists(HubPaths.Adb))
        {
            throw new FileNotFoundException("adb is not installed yet.", HubPaths.Adb);
        }

        var start = new ProcessStartInfo
        {
            FileName = HubPaths.Adb,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(start) ?? throw new InvalidOperationException("adb failed to start.");
        var stdout = await p.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await p.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var combined = (stdout + Environment.NewLine + stderr).Trim();
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException(combined);
        }

        return combined;
    }
}
