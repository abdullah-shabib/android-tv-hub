using AndroidTvHub.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AndroidTvHub;

public sealed partial class MainWindow : Window
{
    private readonly HubSettings _settings;
    private readonly QemuLocator _locator = new();
    private readonly QemuLauncher _launcher = new();
    private readonly GuestImageClient _images = new();
    private readonly AdbClient _adb = new();
    private readonly CancellationTokenSource _lifetime = new();

    public MainWindow()
    {
        InitializeComponent();
        _settings = HubSettings.Load();
        _launcher.Exited += (_, _) => DispatcherQueue.TryEnqueue(RefreshButtons);
        CpuBox.Value = _settings.CpuCores;
        RamBox.Value = _settings.RamMb;
        DisplayCombo.SelectedIndex = _settings.DisplayMode == DisplayMode.ExclusiveFullscreen ? 1 : 0;
        UiSizeCombo.SelectedIndex = _settings.UiWidth >= 3840 ? 1 : 0;
        Log("Hub control surface ready. QEMU owns the Guest window.");
        RefreshStatus();
        RefreshButtons();
    }

    private void SettingsChanged(object sender, object args)
    {
        _settings.CpuCores = (int)Math.Clamp(CpuBox.Value, 2, 16);
        _settings.RamMb = (int)Math.Clamp(RamBox.Value, 2048, 16384);
        if (DisplayCombo.SelectedItem is ComboBoxItem display && display.Tag is string tag)
        {
            _settings.DisplayMode = tag == "ExclusiveFullscreen"
                ? DisplayMode.ExclusiveFullscreen
                : DisplayMode.Windowed;
        }

        if (UiSizeCombo.SelectedItem is ComboBoxItem ui && ui.Tag is string size)
        {
            if (size == "2160")
            {
                _settings.UiWidth = 3840;
                _settings.UiHeight = 2160;
            }
            else
            {
                _settings.UiWidth = 1920;
                _settings.UiHeight = 1080;
            }
        }

        _settings.Save();
    }

    private async void DownloadClick(object sender, RoutedEventArgs e)
    {
        DownloadButton.IsEnabled = false;
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.Value = 0;
        var log = new Progress<string>(Log);
        var pct = new Progress<double>(p => DownloadProgress.Value = p);
        try
        {
            await _images.DownloadAsync(log, pct, _lifetime.Token);
            RefreshStatus();
        }
        catch (Exception ex)
        {
            Log("Download failed: " + ex.Message);
        }
        finally
        {
            DownloadButton.IsEnabled = true;
            RefreshButtons();
        }
    }

    private async void StartClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_locator.TryFind(_settings) || _locator.QemuSystem is null || _locator.QemuImg is null)
            {
                Log("QEMU not found. Install with: winget install SoftwareFreedomConservancy.QEMU");
                return;
            }

            if (!await _images.IsPresentAndValidAsync(_lifetime.Token))
            {
                Log("Guest ISO missing or checksum mismatch. Download it first.");
                return;
            }

            try
            {
                BootMedia.ExtractFromIso(HubPaths.GuestIso, new Progress<string>(Log));
            }
            catch (Exception ex)
            {
                Log("Boot file extract failed (will try ISO/GRUB): " + ex.Message);
            }

            var whpx = WhpxProbe.Probe(_locator.QemuSystem);
            if (!whpx.AcceleratorAccepted)
            {
                Log("WHPX not usable: " + whpx.Detail);
                return;
            }

            _launcher.Start(_locator.QemuSystem, _locator.QemuImg, _settings, HubPaths.GuestIso, HubPaths.GuestDisk);
            Log("Started QEMU. Use the Guest window for Leanback. Keyboard and SDL gamepads go to that window.");
            Log("ADB will be forwarded to 127.0.0.1:" + _settings.AdbHostPort + " once the Guest boots.");
            RefreshButtons();
        }
        catch (Exception ex)
        {
            Log("Start failed: " + ex.Message);
        }
    }

    private void StopClick(object sender, RoutedEventArgs e)
    {
        _launcher.Stop();
        Log("Stopped QEMU.");
        RefreshButtons();
    }

    private async void SideloadClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".apk");
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            var log = new Progress<string>(Log);
            await _adb.EnsureAdbAsync(log, _lifetime.Token);
            Log(await _adb.ConnectAsync(_settings.AdbHostPort, _lifetime.Token));
            Log(await _adb.SideloadAsync(file.Path, _settings.AdbHostPort, _lifetime.Token));
        }
        catch (Exception ex)
        {
            Log("Sideload failed: " + ex.Message);
        }
    }

    private void RefreshStatus()
    {
        if (_locator.TryFind(_settings) && _locator.QemuSystem is not null)
        {
            var version = QemuLocator.Version(_locator.QemuSystem) ?? _locator.QemuSystem;
            QemuStatusText.Text = "QEMU: " + version;
            WhpxStatusText.Text = "WHPX: listed in this QEMU (probed on Start)";
        }
        else
        {
            QemuStatusText.Text = "QEMU: not found (winget install SoftwareFreedomConservancy.QEMU)";
            WhpxStatusText.Text = "WHPX: unknown until QEMU is installed";
        }

        ImageStatusText.Text = File.Exists(HubPaths.GuestIso)
            ? "Guest ISO: " + HubPaths.GuestIso
            : "Guest ISO: not downloaded (" + GuestCatalog.Id + ")";
    }

    private void RefreshButtons()
    {
        var running = _launcher.IsRunning;
        StartButton.IsEnabled = !running;
        StopButton.IsEnabled = running;
        SideloadButton.IsEnabled = running;
    }

    private void Log(string line)
    {
        var stamp = DateTime.Now.ToString("HH:mm:ss");
        LogText.Text += $"[{stamp}] {line}{Environment.NewLine}";
    }
}
