using System;
using System.Runtime.InteropServices;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Screentation;

/// <summary>
/// Manages the system tray icon using Win32 Shell_NotifyIcon API directly.
/// This avoids all H.NotifyIcon/WinUI flyout issues where Click/Command bindings
/// never fire because MenuFlyout is outside the visual tree.
/// </summary>
internal sealed class TrayManager : IDisposable
{
    #region Win32 constants

    private const int  GWLP_WNDPROC      = -4;
    private const uint WM_LBUTTONUP      = 0x0202;
    private const uint WM_LBUTTONDBLCLK  = 0x0203;
    private const uint WM_RBUTTONUP      = 0x0205;
    private const uint WM_CONTEXTMENU    = 0x007B;
    private const uint WM_USER           = 0x0400;
    private const uint TRAY_MSG          = WM_USER + 100;

    private const uint NIF_MESSAGE  = 0x01;
    private const uint NIF_ICON     = 0x02;
    private const uint NIF_TIP      = 0x04;
    private const uint NIM_ADD      = 0x00;
    private const uint NIM_DELETE   = 0x02;

    private const uint IMAGE_ICON       = 1;
    private const uint LR_LOADFROMFILE  = 0x10;
    private const uint LR_DEFAULTSIZE   = 0x40;

    private const uint MF_STRING    = 0x00;
    private const uint MF_SEPARATOR = 0x800;
    private const uint TPM_RETURNCMD   = 0x100;
    private const uint TPM_RIGHTBUTTON = 0x002;

    private const uint ICON_ID  = 1;
    private const int  CMD_OPEN = 100;
    private const int  CMD_EXIT = 101;

    #endregion

    #region Win32 structs

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    #endregion

    #region Win32 imports

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wP, IntPtr lP);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hinst, string name, uint type, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int smIndex);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uID, string? text);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int res, IntPtr hWnd, IntPtr rc);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT pt);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    #endregion

    // Fields — all must be stored to prevent GC collection
    private readonly IntPtr          _hwnd;
    private readonly IntPtr          _hIcon;
    private readonly IntPtr          _oldWndProc;
    private readonly WndProcDelegate _wndProcDelegate; // GC anchor for the delegate!
    private readonly Action          _onOpen;
    private readonly Action          _onExit;
    private readonly string          _labelOpen;
    private readonly string          _labelExit;
    private bool _disposed;

    public TrayManager(IntPtr hwnd, string iconPath, string tooltip, Action onOpen, Action onExit)
    {
        _hwnd   = hwnd;
        _onOpen = onOpen;
        _onExit = onExit;

        // Pre-load localized strings so ResourceLoader isn't called on WndProc thread
        try
        {
            var rl = new ResourceLoader();
            _labelOpen = rl.GetString("TrayOpen/Text");
            _labelExit = rl.GetString("TrayExit/Text");
        }
        catch { }
        if (string.IsNullOrEmpty(_labelOpen)) _labelOpen = "Open Screentation";
        if (string.IsNullOrEmpty(_labelExit)) _labelExit = "Exit";

        // Load .ico file from disk with exact small icon metrics (high DPI aware)
        int cx = GetSystemMetrics(49); // SM_CXSMICON
        int cy = GetSystemMetrics(50); // SM_CYSMICON
        _hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, cx, cy, LR_LOADFROMFILE);

        // Subclass the window proc — the delegate MUST be kept in a field or GC
        // will collect the function pointer and the app will crash when Windows calls it.
        _wndProcDelegate = TrayWndProc;
        _oldWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

        // Register tray icon with Windows Shell
        var nid = new NOTIFYICONDATA
        {
            cbSize          = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd            = _hwnd,
            uID             = ICON_ID,
            uFlags          = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage= TRAY_MSG,
            hIcon           = _hIcon,
            szTip           = tooltip
        };
        Shell_NotifyIcon(NIM_ADD, ref nid);
    }

    private IntPtr TrayWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (uMsg == TRAY_MSG)
        {
            // Low 16 bits of lParam = mouse/notification event code
            uint evt = (uint)(lParam.ToInt64() & 0xFFFF);

            switch (evt)
            {
                case WM_RBUTTONUP:
                case WM_CONTEXTMENU:
                    ShowNativeMenu();
                    return IntPtr.Zero;

                case WM_LBUTTONDBLCLK:
                case WM_LBUTTONUP:
                    _onOpen();
                    return IntPtr.Zero;
            }
        }

        // Forward everything else to WinUI 3's original window proc
        return CallWindowProc(_oldWndProc, hWnd, uMsg, wParam, lParam);
    }

    private void ShowNativeMenu()
    {
        IntPtr hMenu = CreatePopupMenu();
        AppendMenu(hMenu, MF_STRING,    new IntPtr(CMD_OPEN), _labelOpen);
        AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, null);
        AppendMenu(hMenu, MF_STRING,    new IntPtr(CMD_EXIT), _labelExit);

        // SetForegroundWindow is required so the menu dismisses when clicking outside it
        SetForegroundWindow(_hwnd);
        GetCursorPos(out var pt);

        int cmd = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
        DestroyMenu(hMenu);

        if      (cmd == CMD_OPEN) _onOpen();
        else if (cmd == CMD_EXIT) _onExit();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Remove tray icon from the taskbar notification area
        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd   = _hwnd,
            uID    = ICON_ID
        };
        Shell_NotifyIcon(NIM_DELETE, ref nid);

        if (_hIcon != IntPtr.Zero)    DestroyIcon(_hIcon);
        if (_oldWndProc != IntPtr.Zero) SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _oldWndProc);
    }
}
