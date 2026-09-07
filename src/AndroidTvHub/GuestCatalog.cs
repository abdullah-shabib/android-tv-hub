namespace AndroidTvHub;

/// <summary>
/// Pinned upstream Lineage Android TV x86 ISO. Not stored in git.
/// </summary>
internal static class GuestCatalog
{
    public const string Id = "lineage-21.0-20260331-UNOFFICIAL-x86_64_tv";
    public const string FileName = "lineage-21.0-20260331-UNOFFICIAL-x86_64_tv-signed.iso";
    public const string Sha256 = "29c44bb7bb0cb6531a11e3778377c985c4c96b881b2666fba3901e4c21d67bc2";
    public const string DownloadUrl =
        "https://sourceforge.net/projects/lineageos-tv-x86/files/lineage-21.0/x86_64_tv/lineage-21.0-20260331-UNOFFICIAL-x86_64_tv-signed.iso/download";
    public const string ProjectUrl = "https://github.com/LineageOS-TV-x86";
    public const string KernelAppend =
        "root=/dev/ram0 androidboot.live=true ROOT=LABEL=LineageOS_20260331 androidboot.selinux=permissive";
}
