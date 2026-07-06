using System.Drawing;
using System.Runtime.InteropServices;
using PowerID.Utilities;
using PowerID.ViewModels;

namespace PowerID.Services;

/// <summary>
/// Native Win32 system tray icon, menu, and callbacks. WinUI 3 has no built-in notification-area
/// API, so this talks to Shell_NotifyIcon directly through a hidden message-only window - the
/// Windows equivalent of the NSStatusItem driven by MenuBarManager.swift on macOS.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private const int WmTrayCallback = 0x8001; // WM_APP + 1
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonUp = 0x0205;
    private const int WmDestroy = 0x0002;
    private const int WmNull = 0x0000;

    private const uint NimAdd = 0x00000000;
    private const uint NimModify = 0x00000001;
    private const uint NimDelete = 0x00000002;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;

    private const uint MfString = 0x00000000;
    private const uint MfGrayed = 0x00000001;
    private const uint MfSeparator = 0x00000800;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmReturnCmd = 0x0100;

    private const int CmdShow = 1003;
    private const int CmdQuit = 1004;

    private readonly BatteryMonitor _batteryMonitor;
    private readonly Action _onShowRequested;
    private readonly Action _onQuitRequested;

    private WndProcDelegate? _wndProcDelegate;
    private IntPtr _hwnd;
    private IntPtr _currentIconHandle;
    private bool _added;

    public TrayIconService(BatteryMonitor batteryMonitor, Action onShowRequested, Action onQuitRequested)
    {
        _batteryMonitor = batteryMonitor;
        _onShowRequested = onShowRequested;
        _onQuitRequested = onQuitRequested;
    }

    public void Enable()
    {
        if (_added) return;

        CreateHiddenWindow();
        UpdateIconAndTooltip();
        AddIcon();
        _batteryMonitor.PropertyChanged += (_, _) => UpdateIconAndTooltip();
    }

    public void Disable()
    {
        if (!_added) return;

        var data = NewIconData();
        NativeMethods.Shell_NotifyIcon(NimDelete, ref data);
        _added = false;

        FreeCurrentIcon();

        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    private void CreateHiddenWindow()
    {
        _wndProcDelegate = WndProc;
        var wndClass = new WndClassEx
        {
            cbSize = Marshal.SizeOf<WndClassEx>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = NativeMethods.GetModuleHandle(null),
            lpszClassName = "PowerIDTrayWindow",
        };
        NativeMethods.RegisterClassEx(ref wndClass);

        _hwnd = NativeMethods.CreateWindowEx(
            0, "PowerIDTrayWindow", "PowerID Tray", 0,
            0, 0, 0, 0,
            new IntPtr(-3) /* HWND_MESSAGE */, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);
    }

    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WmTrayCallback:
                var callbackEvent = lParam.ToInt32();
                if (callbackEvent is WmLButtonUp or WmRButtonUp)
                {
                    ShowContextMenu();
                }
                return IntPtr.Zero;

            case WmDestroy:
                return IntPtr.Zero;

            default:
                return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }

    private void HandleCommand(int id)
    {
        switch (id)
        {
            case CmdShow:
                _onShowRequested();
                break;
            case CmdQuit:
                _onQuitRequested();
                break;
        }
    }

    private void ShowContextMenu()
    {
        var menu = NativeMethods.CreatePopupMenu();

        var statusText = _batteryMonitor.IsCharging ? "Charging" : "On Battery";
        NativeMethods.AppendMenu(menu, MfString | MfGrayed, (UIntPtr)1000, $"Battery: {_batteryMonitor.BatteryLevel}%");
        NativeMethods.AppendMenu(menu, MfString | MfGrayed, (UIntPtr)1001, $"Status: {statusText}");
        NativeMethods.AppendMenu(menu, MfString | MfGrayed, (UIntPtr)1002, $"Health: {_batteryMonitor.BatteryHealth}%");
        NativeMethods.AppendMenu(menu, MfSeparator, UIntPtr.Zero, string.Empty);
        NativeMethods.AppendMenu(menu, MfString, (UIntPtr)CmdShow, "Show PowerID");
        NativeMethods.AppendMenu(menu, MfSeparator, UIntPtr.Zero, string.Empty);
        NativeMethods.AppendMenu(menu, MfString, (UIntPtr)CmdQuit, "Quit PowerID");

        NativeMethods.GetCursorPos(out var point);
        NativeMethods.SetForegroundWindow(_hwnd);
        // TPM_RETURNCMD makes TrackPopupMenuEx return the chosen item's ID directly instead of
        // posting WM_COMMAND to _hwnd, so the selection is handled right here.
        var selectedCommand = NativeMethods.TrackPopupMenuEx(menu, TpmRightButton | TpmReturnCmd, point.X, point.Y, _hwnd, IntPtr.Zero);
        NativeMethods.PostMessage(_hwnd, WmNull, IntPtr.Zero, IntPtr.Zero);
        NativeMethods.DestroyMenu(menu);

        if (selectedCommand != 0)
        {
            HandleCommand(selectedCommand);
        }
    }

    private void AddIcon()
    {
        var data = NewIconData();
        data.uFlags = NifMessage | NifIcon | NifTip;
        NativeMethods.Shell_NotifyIcon(NimAdd, ref data);
        _added = true;
    }

    private void UpdateIconAndTooltip()
    {
        if (_hwnd == IntPtr.Zero) return;

        FreeCurrentIcon();
        _currentIconHandle = DrawIcon(_batteryMonitor.BatteryLevel, _batteryMonitor.IsCharging);

        if (!_added) return;

        var data = NewIconData();
        data.uFlags = NifIcon | NifTip;
        NativeMethods.Shell_NotifyIcon(NimModify, ref data);
    }

    private NotifyIconData NewIconData()
    {
        var statusText = _batteryMonitor.IsCharging ? "Charging" : "On Battery";
        return new NotifyIconData
        {
            cbSize = Marshal.SizeOf<NotifyIconData>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NifIcon | NifTip | NifMessage,
            uCallbackMessage = WmTrayCallback,
            hIcon = _currentIconHandle,
            szTip = $"PowerID - {_batteryMonitor.BatteryLevel}% ({statusText})",
        };
    }

    /// <summary>Renders a small battery glyph with the current percentage, colored like the in-app gradient.</summary>
    private static IntPtr DrawIcon(int level, bool isCharging)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var (startColor, _) = BatteryFormatter.BatteryGradientColors(level, isCharging);
            var fillColor = Color.FromArgb(startColor.A, startColor.R, startColor.G, startColor.B);

            using var bodyPen = new Pen(Color.White, 2f);
            var bodyRect = new Rectangle(2, 8, 24, 16);
            g.DrawRectangle(bodyPen, bodyRect);
            g.FillRectangle(new SolidBrush(Color.White), 26, 13, 3, 6);

            var fillWidth = (int)Math.Round((bodyRect.Width - 4) * Math.Clamp(level, 0, 100) / 100.0);
            if (fillWidth > 0)
            {
                using var fillBrush = new SolidBrush(fillColor);
                g.FillRectangle(fillBrush, bodyRect.X + 2, bodyRect.Y + 2, fillWidth, bodyRect.Height - 4);
            }
        }

        return bitmap.GetHicon();
    }

    private void FreeCurrentIcon()
    {
        if (_currentIconHandle != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_currentIconHandle);
            _currentIconHandle = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Disable();
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public int cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public uint uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct
    {
        public int X;
        public int Y;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ushort RegisterClassEx(ref WndClassEx lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("shell32.dll")]
        public static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

        [DllImport("user32.dll")]
        public static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string? lpNewItem);

        [DllImport("user32.dll")]
        public static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out PointStruct lpPoint);

        [DllImport("user32.dll")]
        public static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }
}
