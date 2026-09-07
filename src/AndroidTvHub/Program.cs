using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace AndroidTvHub;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // Unpackaged + WindowsAppSDKSelfContained copies WinUI next to this exe.
        // Do not call Bootstrap.TryInitialize: that looks for the MSIX Windows App
        // Runtime and shows "This application could not be started" when it is absent.
        Environment.SetEnvironmentVariable(
            "MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY",
            AppContext.BaseDirectory);

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
        return 0;
    }
}
