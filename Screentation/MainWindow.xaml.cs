using System;
using System.IO;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Screentation;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    private TrayManager? _trayManager;
    private bool _isExiting = false;
    private bool _trayInitialized = false;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Set window/taskbar icon at runtime
        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Intercept X button — hide to tray instead of closing
        AppWindow.Closing += AppWindow_Closing;

        // Initialize tray AFTER window is shown (HWND must be fully created)
        this.Activated += OnFirstActivated;

        RootFrame.Navigate(typeof(MainPage));
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        // Only run once
        if (_trayInitialized) return;
        _trayInitialized = true;

        var hwnd = WindowNative.GetWindowHandle(this);

        // Resolve icon path: in publish layout, Assets/ is next to the .exe
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string iconPath = Path.Combine(baseDir, "Assets", "AppIcon.ico");

        _trayManager = new TrayManager(
            hwnd:     hwnd,
            iconPath: iconPath,
            tooltip:  "Screentation",
            onOpen:   RestoreWindow,
            onExit:   ExitApplication);
    }

    private void AppWindow_Closing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;   // Don't close the process
            AppWindow.Hide();  // Just hide the window
        }
    }

    public void RestoreWindow()
    {
        AppWindow.Show();
        this.Activate();
    }

    public void ExitApplication()
    {
        _isExiting = true;

        // Dispose removes the tray icon from the notification area
        try { _trayManager?.Dispose(); } catch { }

        // Force-terminate the process including all background threads
        Environment.Exit(0);
    }
}
