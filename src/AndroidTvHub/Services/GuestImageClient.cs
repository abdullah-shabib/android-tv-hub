using System.Security.Cryptography;

namespace AndroidTvHub.Services;

internal sealed class GuestImageClient
{
    public async Task<bool> IsPresentAndValidAsync(CancellationToken cancellationToken)
    {
        var path = HubPaths.GuestIso;
        if (!File.Exists(path))
        {
            return false;
        }

        var hash = await HashFileAsync(path, cancellationToken).ConfigureAwait(false);
        return hash.Equals(GuestCatalog.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    public async Task DownloadAsync(IProgress<string> log, IProgress<double>? percent, CancellationToken cancellationToken)
    {
        HubPaths.EnsureLayout();
        var dest = HubPaths.GuestIso;
        var temp = dest + ".partial";

        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AndroidTvHub/0.1");

        log.Report("Downloading " + GuestCatalog.FileName + " from upstream…");
        using var response = await http.GetAsync(GuestCatalog.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 256, useAsync: true);

        var buffer = new byte[1024 * 256];
        long copied = 0;
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            copied += read;
            if (total is > 0)
            {
                percent?.Report(copied / (double)total * 100.0);
            }
        }

        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        output.Close();

        log.Report("Verifying SHA-256…");
        var hash = await HashFileAsync(temp, cancellationToken).ConfigureAwait(false);
        if (!hash.Equals(GuestCatalog.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(temp);
            throw new InvalidOperationException($"SHA-256 mismatch. Expected {GuestCatalog.Sha256}, got {hash}.");
        }

        if (File.Exists(dest))
        {
            File.Delete(dest);
        }

        File.Move(temp, dest);
        log.Report("Guest ISO ready.");
        BootMedia.ExtractFromIso(dest, log);
        percent?.Report(100);
    }

    private static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 256, useAsync: true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
