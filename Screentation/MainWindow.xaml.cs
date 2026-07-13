using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace Screentation;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }
    private bool _isExiting = false;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Intercept closing event to hide the window
        AppWindow.Closing += AppWindow_Closing;

        // Hook double click command on tray icon
        MyTaskbarIcon.DoubleClickCommand = new TrayRelayCommand(RestoreWindow);

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
    }

    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true; // Prevent application exit
            AppWindow.Hide(); // Hide main window
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
        MyTaskbarIcon.Dispose();
        Application.Current.Exit();
        Environment.Exit(0);
    }

    private void OpenItem_Click(object sender, RoutedEventArgs e)
    {
        RestoreWindow();
    }

    private void ExitItem_Click(object sender, RoutedEventArgs e)
    {
        ExitApplication();
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
