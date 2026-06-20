using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

namespace Screentation;

public partial class App : Application
{
    private Window? _window;
    
    public App()
    {
        InitializeComponent();
        
        // Hook unhandled exceptions
        this.UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt");
            string errorText = $"=== CRASH LOG ===\n" +
                               $"Time: {DateTime.Now}\n" +
                               $"Message: {e.Message}\n" +
                               $"Exception: {e.Exception?.ToString()}\n" +
                               $"Inner Exception: {e.Exception?.InnerException?.ToString()}\n" +
                               $"Stack Trace: {e.Exception?.StackTrace}\n" +
                               $"Handled: {e.Handled}\n";
            File.WriteAllText(logPath, errorText);
            
            // Also write to project directory if possible
            string projectLogPath = @"D:\Develop\ScreenTort\Screentation\crash.txt";
            File.WriteAllText(projectLogPath, errorText);
        }
        catch
        {
            // Ignore log write failures
        }
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            // Catch startup exceptions and write to log
            string logPath = @"D:\Develop\ScreenTort\Screentation\crash.txt";
            string errorText = $"=== STARTUP CRASH LOG ===\n" +
                               $"Time: {DateTime.Now}\n" +
                               $"Message: {ex.Message}\n" +
                               $"Exception: {ex}\n" +
                               $"Inner Exception: {ex.InnerException?.ToString()}\n" +
                               $"Stack Trace: {ex.StackTrace}\n";
            File.WriteAllText(logPath, errorText);
            throw;
        }
    }
}
