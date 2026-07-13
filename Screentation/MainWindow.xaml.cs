using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Screentation;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    // Static reference prevents GC from collecting the tray icon
    private static H.NotifyIcon.TaskbarIcon? _trayIconRef;
    private bool _isExiting = false;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Intercept window close button — hide to tray instead of exiting
        AppWindow.Closing += AppWindow_Closing;

        // Build tray context menu entirely in code-behind.
        // XAML ElementName bindings do NOT work inside ContextFlyout because
        // MenuFlyout is not part of the visual tree of the Window.
        _BuildTrayMenu();

        // Double-click on tray icon restores the window
        MyTaskbarIcon.DoubleClickCommand = new TrayRelayCommand(RestoreWindow);

        // Keep a static strong reference to prevent garbage collection
        _trayIconRef = MyTaskbarIcon;

        // Navigate to main page
        RootFrame.Navigate(typeof(MainPage));
    }

    private void _BuildTrayMenu()
    {
        var loader = new ResourceLoader();

        var openItem = new MenuFlyoutItem
        {
            Text = loader.GetString("TrayOpen/Text")
        };
        openItem.Click += (_, _) => RestoreWindow();

        var exitItem = new MenuFlyoutItem
        {
            Text = loader.GetString("TrayExit/Text")
        };
        exitItem.Click += (_, _) => ExitApplication();

        var flyout = new MenuFlyout();
        flyout.Items.Add(openItem);
        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(exitItem);

        MyTaskbarIcon.ContextFlyout = flyout;
    }

    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs e)
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
        try { _trayIconRef?.Dispose(); } catch { }
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
