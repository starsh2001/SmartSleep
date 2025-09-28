using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace SmartSleep.App.Services;

public class MouseMonitoringService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitoringTask;
    private Forms.NotifyIcon? _notifyIcon;

    public event EventHandler<bool>? MouseOverIconChanged;

    private bool _lastMouseOverState = false;
    private readonly object _stateLock = new object();

    public MouseMonitoringService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void SetNotifyIcon(Forms.NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon;
    }

    public void StartMonitoring()
    {
        if (_monitoringTask != null && !_monitoringTask.IsCompleted)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = Task.Run(MonitorMousePositionAsync, _cancellationTokenSource.Token);
    }

    public void StopMonitoring()
    {
        _cancellationTokenSource?.Cancel();
        _monitoringTask?.Wait(TimeSpan.FromSeconds(1));
    }

    private async Task MonitorMousePositionAsync()
    {
        while (!_cancellationTokenSource?.Token.IsCancellationRequested == true)
        {
            try
            {
                bool currentMouseOver = IsMouseOverIcon();

                lock (_stateLock)
                {
                    if (currentMouseOver != _lastMouseOverState)
                    {
                        _lastMouseOverState = currentMouseOver;
                        System.Diagnostics.Debug.WriteLine($"MouseMonitoringService: Mouse state changed to {currentMouseOver}");

                        // Notify on UI thread
                        _dispatcher.BeginInvoke(() =>
                        {
                            MouseOverIconChanged?.Invoke(this, currentMouseOver);
                        });
                    }
                }

                // Check every 50ms for smooth experience
                await Task.Delay(50, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Continue monitoring even if there's an error
                await Task.Delay(100, _cancellationTokenSource.Token);
            }
        }
    }

    private bool IsMouseOverIcon()
    {
        if (_notifyIcon == null)
            return false;

        try
        {
            var mousePos = Forms.Control.MousePosition;

            // Try to get the exact NotifyIcon rectangle using Windows API
            if (TryGetNotifyIconRect(_notifyIcon, out var iconRect))
            {
                // Add some padding around the icon for better UX
                var paddedRect = new Rectangle(
                    iconRect.X - 5,
                    iconRect.Y - 5,
                    iconRect.Width + 10,
                    iconRect.Height + 10
                );

                return paddedRect.Contains(mousePos);
            }
            else
            {
                // Fallback: use approximate tray area detection
                var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;

                var trayArea = new Rectangle(
                    screenBounds.Width - 100,
                    workingArea.Bottom,
                    100,
                    screenBounds.Height - workingArea.Bottom
                );

                return trayArea.Contains(mousePos);
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetNotifyIconRect(Forms.NotifyIcon icon, out Rectangle rect)
    {
        rect = Rectangle.Empty;

        try
        {
            if (!NotifyIconHelper.TryBuildData(icon, out var data))
                return false;

            var identifier = new NotifyIconHelper.NOTIFYICONIDENTIFIER
            {
                cbSize = Marshal.SizeOf<NotifyIconHelper.NOTIFYICONIDENTIFIER>(),
                hWnd = data.hWnd,
                uID = data.uID,
                guidItem = data.guidItem
            };

            var result = NotifyIconHelper.Shell_NotifyIconGetRect(ref identifier, out NotifyIconHelper.RECT iconRect);
            if (result == IntPtr.Zero) // S_OK
            {
                rect = new Rectangle(iconRect.Left, iconRect.Top, iconRect.Width, iconRect.Height);
                return true;
            }
        }
        catch
        {
            // Fall back to detection method
        }

        return false;
    }

    public void Dispose()
    {
        StopMonitoring();
        _cancellationTokenSource?.Dispose();
    }
}

// Helper class for NotifyIcon API access
internal static class NotifyIconHelper
{
    private static readonly System.Reflection.FieldInfo? IdField = typeof(Forms.NotifyIcon).GetField("id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    private static readonly System.Reflection.FieldInfo? WindowField = typeof(Forms.NotifyIcon).GetField("window", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    private static readonly System.Reflection.MethodInfo? CreateHandleMethod = typeof(Forms.NotifyIcon).GetMethod("CreateHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    internal static bool TryBuildData(Forms.NotifyIcon icon, out NOTIFYICONDATA data)
    {
        data = NOTIFYICONDATA.Create();

        try
        {
            var window = WindowField?.GetValue(icon) as Forms.NativeWindow;
            if (window == null)
            {
                CreateHandleMethod?.Invoke(icon, null);
                window = WindowField?.GetValue(icon) as Forms.NativeWindow;
            }

            var handle = window?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero)
            {
                data = default;
                return false;
            }

            var idValue = IdField?.GetValue(icon);
            if (idValue == null)
            {
                data = default;
                return false;
            }

            data.hWnd = handle;
            data.uID = Convert.ToUInt32(idValue);
            return true;
        }
        catch
        {
            data = default;
            return false;
        }
    }

    [DllImport("shell32.dll")]
    internal static extern IntPtr Shell_NotifyIconGetRect([In] ref NOTIFYICONIDENTIFIER identifier, [Out] out RECT iconLocation);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NOTIFYICONDATA
    {
        public int cbSize;
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

        internal static NOTIFYICONDATA Create()
        {
            return new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                szTip = string.Empty,
                szInfo = string.Empty,
                szInfoTitle = string.Empty,
                guidItem = Guid.Empty,
                hBalloonIcon = IntPtr.Zero
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NOTIFYICONIDENTIFIER
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public Guid guidItem;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }
}