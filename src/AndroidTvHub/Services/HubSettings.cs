using System.Text.Json;
using System.Text.Json.Serialization;

namespace AndroidTvHub.Services;

internal enum DisplayMode
{
    Windowed,
    ExclusiveFullscreen
}

internal sealed class HubSettings
{
    public int CpuCores { get; set; } = Math.Clamp(Environment.ProcessorCount / 2, 2, 8);
    public int RamMb { get; set; } = 4096;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Windowed;
    public int UiWidth { get; set; } = 1920;
    public int UiHeight { get; set; } = 1080;
    public int AdbHostPort { get; set; } = 5555;
    public string? QemuDirectory { get; set; }

    public static HubSettings Load()
    {
        HubPaths.EnsureLayout();
        if (!File.Exists(HubPaths.SettingsFile))
        {
            var created = new HubSettings();
            created.Save();
            return created;
        }

        var json = File.ReadAllText(HubPaths.SettingsFile);
        return JsonSerializer.Deserialize(json, HubJsonContext.Default.HubSettings) ?? new HubSettings();
    }

    public void Save()
    {
        HubPaths.EnsureLayout();
        var json = JsonSerializer.Serialize(this, HubJsonContext.Default.HubSettings);
        File.WriteAllText(HubPaths.SettingsFile, json);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(HubSettings))]
internal partial class HubJsonContext : JsonSerializerContext;
