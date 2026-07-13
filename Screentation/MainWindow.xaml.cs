using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace Screentation;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    // Keep a static reference to prevent GC from collecting the tray icon
    private static H.NotifyIcon.TaskbarIcon? _trayIcon;
    private bool _isExiting = false;

    // Commands exposed for XAML binding
    public ICommand OpenCommand { get; }
    public ICommand ExitCommand { get; }

    public MainWindow()
    {
        Instance = this;

        // Build commands before InitializeComponent so XAML bindings resolve
        OpenCommand = new TrayRelayCommand(RestoreWindow);
        ExitCommand = new TrayRelayCommand(ExitApplication);

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Intercept closing event to hide the window instead of closing it
        AppWindow.Closing += AppWindow_Closing;

        // Store strong reference and hook double-click
        _trayIcon = MyTaskbarIcon;
        MyTaskbarIcon.DoubleClickCommand = OpenCommand;

        // Navigate the root frame to the main page on startup
        RootFrame.Navigate(typeof(MainPage));
    }

    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            AppWindow.Hide();
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
        try { MyTaskbarIcon.Dispose(); } catch { }
        try { _trayIcon?.Dispose(); } catch { }
        Environment.Exit(0);
    }
}

public class TrayRelayCommand : ICommand
{
    private readonly Action _execute;

    public TrayRelayCommand(Action execute)
    {
        _execute = execute;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged;
}
