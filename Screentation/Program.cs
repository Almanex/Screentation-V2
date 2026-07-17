using System;
using System.IO.Pipes;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Screentation;

public static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    static void Main(string[] args)
    {
        bool isNewInstance;
        _mutex = new Mutex(true, "Global\\Screentation-v2-SingleInstanceMutex", out isNewInstance);

        if (!isNewInstance)
        {
            // Notify the running instance via Named Pipe
            try
            {
                using var client = new NamedPipeClientStream(".", "ScreentationSingleInstancePipe", PipeDirection.Out);
                client.Connect(1000); // 1 second timeout
            }
            catch
            {
                // Fallback: do nothing if the first instance is unresponsive or closing
            }
            return;
        }

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
