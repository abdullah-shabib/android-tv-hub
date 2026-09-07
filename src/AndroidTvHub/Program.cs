using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace AndroidTvHub;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        var min = new PackageVersion();
        if (!Bootstrap.TryInitialize(0x00010006, string.Empty, min, Bootstrap.InitializeOptions.OnNoMatch_ShowUI, out var hr))
        {
            var msg = "Windows App SDK 1.6 failed to start (HRESULT 0x" + hr.ToString("X8") + "). Install it with: winget install Microsoft.WindowsAppRuntime.1.6";
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "android-tv-hub-bootstrap.txt"), msg);
            NativeMessageBox(IntPtr.Zero, msg, "Android TV Hub", 0x00000010);
            return 1;
        }

        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
        Bootstrap.Shutdown();
        return 0;
    }

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int NativeMessageBox(IntPtr hWnd, string text, string caption, uint type);
}
