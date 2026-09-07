using AndroidTvHub.Services;

namespace AndroidTvHub;

internal static class HubPaths
{
    public static string Root { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidTvHub");

    public static string Images => Path.Combine(Root, "images");
    public static string Disks => Path.Combine(Root, "disks");
    public static string Tools => Path.Combine(Root, "tools");
    public static string Logs => Path.Combine(Root, "logs");
    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string GuestIso => Path.Combine(Images, GuestCatalog.FileName);
    public static string GuestDisk => Path.Combine(Disks, "tv-guest.qcow2");
    public static string PlatformTools => Path.Combine(Tools, "platform-tools");
    public static string Adb => Path.Combine(PlatformTools, "adb.exe");
    public static string QemuImgDefault => Path.Combine(Tools, "qemu");
    public static string Boot => Path.Combine(Root, "boot");
    public static string Kernel => Path.Combine(Boot, "kernel");
    public static string Initrd => Path.Combine(Boot, "initrd.img");

    public static void EnsureLayout()
    {
        Directory.CreateDirectory(Images);
        Directory.CreateDirectory(Disks);
        Directory.CreateDirectory(Tools);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Boot);
    }
}
