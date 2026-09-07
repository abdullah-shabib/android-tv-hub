using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace AndroidTvHub.Services;

internal sealed class QemuLauncher : IDisposable
{
    private Process? _process;

    public bool IsRunning => _process is { HasExited: false };

    public event EventHandler? Exited;

    public void Start(string qemuSystem, string qemuImg, HubSettings settings, string isoPath, string diskPath)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("Guest is already running.");
        }

        if (!File.Exists(isoPath))
        {
            throw new FileNotFoundException("Guest ISO is missing. Download it first.", isoPath);
        }

        EnsureDisk(qemuImg, diskPath);

        var args = BuildArgs(settings, isoPath, diskPath);
        var start = new ProcessStartInfo
        {
            FileName = qemuSystem,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = false,
            WorkingDirectory = Path.GetDirectoryName(qemuSystem) ?? Environment.CurrentDirectory
        };

        var log = Path.Combine(HubPaths.Logs, "qemu-last-args.txt");
        File.WriteAllText(log, qemuSystem + Environment.NewLine + args + Environment.NewLine);

        _process = Process.Start(start) ?? throw new InvalidOperationException("QEMU failed to start.");
        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) => Exited?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public void Dispose() => Stop();

    public static string BuildArgs(HubSettings settings, string isoPath, string diskPath)
    {
        var display = settings.DisplayMode == DisplayMode.ExclusiveFullscreen
            ? "-full-screen -display sdl,gl=off"
            : "-display sdl,gl=off";

        var sb = new StringBuilder();
        sb.Append("-accel whpx,kernel-irqchip=off ");
        sb.Append("-machine q35 ");
        sb.Append("-cpu qemu64 ");
        sb.Append(CultureInfo.InvariantCulture, $"-smp {settings.CpuCores} ");
        sb.Append(CultureInfo.InvariantCulture, $"-m {settings.RamMb} ");
        sb.Append(CultureInfo.InvariantCulture, $"-device virtio-vga,xres={settings.UiWidth},yres={settings.UiHeight} ");
        sb.Append(display);
        sb.Append(' ');
        sb.Append("-usb -device usb-kbd -device usb-tablet ");
        sb.Append("-device qemu-xhci,id=xhci ");
        sb.Append(CultureInfo.InvariantCulture, $"-netdev user,id=net0,hostfwd=tcp::{settings.AdbHostPort}-:5555 ");
        sb.Append("-device virtio-net-pci,netdev=net0 ");
        sb.Append(CultureInfo.InvariantCulture, $"-drive file=\"{diskPath}\",if=virtio,format=qcow2 ");
        sb.Append(CultureInfo.InvariantCulture, $"-cdrom \"{isoPath}\" ");
        if (BootMedia.IsExtracted)
        {
            sb.Append(CultureInfo.InvariantCulture, $"-kernel \"{HubPaths.Kernel}\" ");
            sb.Append(CultureInfo.InvariantCulture, $"-initrd \"{HubPaths.Initrd}\" ");
            sb.Append(CultureInfo.InvariantCulture, $"-append \"{GuestCatalog.KernelAppend}\" ");
        }
        else
        {
            sb.Append("-boot order=dc,menu=on ");
        }

        sb.Append("-name \"Android TV Guest\"");
        return sb.ToString();
    }

    private static void EnsureDisk(string qemuImg, string diskPath)
    {
        if (File.Exists(diskPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(diskPath)!);
        var start = new ProcessStartInfo
        {
            FileName = qemuImg,
            Arguments = $"create -f qcow2 \"{diskPath}\" 64G",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var p = Process.Start(start) ?? throw new InvalidOperationException("qemu-img failed to start.");
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit(30000);
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException("qemu-img create failed: " + err);
        }
    }
}
