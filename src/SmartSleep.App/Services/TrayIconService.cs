using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using SmartSleep.App.Configuration;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using SmartSleep.App.Models;
using SmartSleep.App.Views;
using SmartSleep.App.Utilities;
using Forms = System.Windows.Forms;

namespace SmartSleep.App.Services;

public class TrayIconService : IDisposable
{
    private readonly MonitoringService _monitoringService;
    private readonly Func<SettingsWindow> _settingsWindowFactory;
    private readonly Dispatcher _dispatcher;
    private Forms.NotifyIcon? _notifyIcon;
    private SettingsWindow? _settingsWindow;
    private System.Drawing.Icon? _iconResource;
    private TrayTooltipWindow? _tooltipWindow;
    private DispatcherTimer? _tooltipHideTimer;
    private DispatcherTimer? _mouseLeaveCheckTimer;
    private bool _systemTooltipSuppressed;
    private MonitoringSnapshot? _lastSnapshot;
    private string _lastTooltipContent = string.Empty;
    private System.Drawing.Point _lastMousePosition = System.Drawing.Point.Empty;

    public TrayIconService(MonitoringService monitoringService,
                           Func<SettingsWindow> settingsWindowFactory,
                           Dispatcher dispatcher)
    {
        _monitoringService = monitoringService;
        _settingsWindowFactory = settingsWindowFactory;
        _dispatcher = dispatcher;
    }

    public void Initialize()
    {
        _iconResource = LoadIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _iconResource ?? System.Drawing.SystemIcons.Application,
            Visible = true
        };

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add(LocalizationManager.GetString("Tray_Menu_OpenSettings"), null, (_, _) => ShowSettings());
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(LocalizationManager.GetString("Tray_Menu_Exit"), null, (_, _) => ExitApplication());
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _monitoringService.SnapshotAvailable += MonitoringServiceOnSnapshotAvailable;
        _monitoringService.SleepTriggered += MonitoringServiceOnSleepTriggered;
        _notifyIcon.MouseMove += NotifyIconOnMouseMove;
        _notifyIcon.MouseDown += NotifyIconOnMouseDown;

        // Subscribe to input activity events

        // Initialize tooltip window immediately
        InitializeTooltipWindow();

        EnsureSystemTooltipSuppressed(force: true);
    }

    private System.Drawing.Icon? LoadIcon()
    {
        try
        {
            var resourceUri = new Uri("pack://application:,,,/resources/smartsleep.ico", UriKind.Absolute);
            var resourceInfo = System.Windows.Application.GetResourceStream(resourceUri);
            if (resourceInfo?.Stream == null)
            {
                return null;
            }

            using var memory = new MemoryStream();
            resourceInfo.Stream.CopyTo(memory);
            memory.Position = 0;
            return new System.Drawing.Icon(memory);
        }
        catch
        {
            return null;
        }
    }

    private void MonitoringServiceOnSnapshotAvailable(object? sender, MonitoringSnapshot snapshot)
    {
        _lastSnapshot = snapshot;

        if (_notifyIcon == null)
        {
            return;
        }

        _dispatcher.BeginInvoke(() =>
        {
            EnsureSystemTooltipSuppressed();

            if (_tooltipWindow?.ShouldRender == true)
            {
                UpdateTooltipWindow();
            }
        });
    }

    private void MonitoringServiceOnSleepTriggered(object? sender, string message)
    {
        if (_notifyIcon == null)
        {
            return;
        }

        _dispatcher.BeginInvoke(() =>
        {
            if (_notifyIcon == null)
            {
                return;
            }

            HideTooltipWindow();

            _notifyIcon.BalloonTipTitle = LocalizationManager.GetString("Tooltip_Title");
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(3000);
        });
    }

    private void NotifyIconOnMouseMove(object? sender, Forms.MouseEventArgs e)
    {
        if (_lastSnapshot == null || _tooltipWindow == null)
        {
            return;
        }

        var currentMousePos = Forms.Control.MousePosition;

        _dispatcher.BeginInvoke(() =>
        {
            if (_lastSnapshot == null || _tooltipWindow == null)
            {
                return;
            }

            // Update last mouse position
            _lastMousePosition = currentMousePos;

            ShowTooltipWindow();
        });
    }

    private void NotifyIconOnMouseDown(object? sender, Forms.MouseEventArgs e)
    {
        _dispatcher.BeginInvoke(HideTooltipWindow);
    }


    private void ShowSettings()
    {
        _dispatcher.BeginInvoke(() =>
        {
            HideTooltipWindow();

            if (_settingsWindow is { IsVisible: true })
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = _settingsWindowFactory();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
        });
    }

    private void ExitApplication()
    {
        _dispatcher.BeginInvoke(new Action(async () =>
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            try
            {
                await _monitoringService.StopAsync().ConfigureAwait(true);
            }
            catch
            {
                // Ignore shutdown exceptions.
            }

            System.Windows.Application.Current.Shutdown();
        }));
    }

    private IReadOnlyList<(string Text, Brush Brush)> BuildTooltipLines(MonitoringSnapshot snapshot)
    {
        var lines = new List<(string Text, Brush Brush)>
        {
            (LocalizationManager.GetString("Tooltip_Title"), Brushes.White)
        };

        if (snapshot.InputMonitoringEnabled)
        {
            var inputText = snapshot.InputActivityDetected
                ? LocalizationManager.GetString("LiveStatus_InputActive") // "입력: 활동 감지됨"
                : LocalizationManager.GetString("LiveStatus_InputWaiting"); // "입력: 대기중"
            var inputBrush = snapshot.InputActivityDetected ? Brushes.Orange : Brushes.White;
            lines.Add((inputText, inputBrush));
        }
        else
        {
            lines.Add((LocalizationManager.GetString("LiveStatus_InputDisabled"), Brushes.DimGray));
        }

        lines.Add(snapshot.CpuMonitoringEnabled
            ? (LocalizationManager.Format("LiveStatus_Cpu", snapshot.CpuUsagePercent, snapshot.CpuThresholdPercent),
               StatusDisplayHelper.GetCpuBrush(snapshot.CpuUsagePercent, snapshot.CpuThresholdPercent, snapshot.ScheduleActive, forTooltip: true))
            : (snapshot.ScheduleActive
                ? LocalizationManager.Format("LiveStatus_CpuDisabled", snapshot.CpuUsagePercent)
                : LocalizationManager.Format("LiveStatus_Cpu", snapshot.CpuUsagePercent, snapshot.CpuThresholdPercent),
               Brushes.DimGray));

        lines.Add(snapshot.NetworkMonitoringEnabled
            ? (LocalizationManager.Format("LiveStatus_Network", snapshot.NetworkKilobytesPerSecond, snapshot.NetworkThresholdKilobytesPerSecond),
               StatusDisplayHelper.GetNetworkBrush(snapshot.NetworkKilobytesPerSecond, snapshot.NetworkThresholdKilobytesPerSecond, snapshot.ScheduleActive, forTooltip: true))
            : (snapshot.ScheduleActive
                ? LocalizationManager.Format("LiveStatus_NetworkDisabled", snapshot.NetworkKilobytesPerSecond)
                : LocalizationManager.Format("LiveStatus_Network", snapshot.NetworkKilobytesPerSecond, snapshot.NetworkThresholdKilobytesPerSecond),
               Brushes.DimGray));

        // Conditions count display removed - no longer needed with real-time activity detection

        if (!string.IsNullOrWhiteSpace(snapshot.StatusMessage))
        {
            // StatusMessage already contains the last-changed message from MonitoringService
            var (statusText, statusBrush) = StatusDisplayHelper.GetStatusMessage(
                snapshot.StatusMessage,
                snapshot.InputActivityDetected,
                snapshot.CpuExceeding,
                snapshot.NetworkExceeding,
                snapshot.ScheduleActive,
                forTooltip: true);

            if (!string.IsNullOrWhiteSpace(statusText))
            {
                lines.Add((statusText, statusBrush));
            }
        }

        return lines;
    }

    private void InitializeTooltipWindow()
    {
        if (_tooltipWindow != null)
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine("Initializing tooltip window");
        _tooltipWindow = new TrayTooltipWindow();

        // Show the window initially, then hide it
        _tooltipWindow.Show();
        _tooltipWindow.SetShouldRender(false);

        _tooltipHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(DefaultValues.TooltipHideDelayMs)
        };
        _tooltipHideTimer.Tick += (_, _) => HideTooltipWindow();

        // Timer to check if mouse left the tray area
        _mouseLeaveCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(DefaultValues.MouseLeaveCheckIntervalMs) // Check every 200ms to reduce flicker
        };
        _mouseLeaveCheckTimer.Tick += CheckMouseLeave;

        System.Diagnostics.Debug.WriteLine("Tooltip window initialized and hidden");
    }

    private void ShowTooltipWindow()
    {
        if (_tooltipWindow == null || _lastSnapshot == null)
        {
            return;
        }

        var lines = BuildTooltipLines(_lastSnapshot);
        var currentContent = string.Join("\n", lines.Select(l => l.Text));
        _tooltipWindow.UpdateLines(lines);
        _lastTooltipContent = currentContent;

        PositionTooltipWindow();
        _tooltipWindow.SetShouldRender(true);

        // Reset the hide timer - hide after 2 seconds if no more mouse movement
        _tooltipHideTimer?.Stop();
        _tooltipHideTimer?.Start();

        // Start checking if mouse leaves the tray area
        _mouseLeaveCheckTimer?.Start();
    }

    private void PositionTooltipWindow()
    {
        if (_tooltipWindow == null)
        {
            return;
        }

        var cursor = Forms.Control.MousePosition;


        var workingArea = SystemParameters.WorkArea;
        _tooltipWindow.Left = cursor.X + 12;
        var desiredTop = cursor.Y - _tooltipWindow.ActualHeight - 12;

        if (desiredTop < workingArea.Top)
        {
            desiredTop = cursor.Y + 12;
        }

        _tooltipWindow.Top = Math.Max(workingArea.Top, Math.Min(desiredTop, workingArea.Bottom - _tooltipWindow.ActualHeight));
        _tooltipWindow.Left = Math.Max(workingArea.Left, Math.Min(_tooltipWindow.Left, workingArea.Right - _tooltipWindow.ActualWidth));
    }

    private void UpdateTooltipWindow()
    {
        if (_tooltipWindow == null || _lastSnapshot == null || !_tooltipWindow.ShouldRender)
        {
            return;
        }

        var lines = BuildTooltipLines(_lastSnapshot);
        var currentContent = string.Join("\n", lines.Select(l => l.Text));

        // Only update if content actually changed to prevent flickering
        if (currentContent != _lastTooltipContent)
        {
            _tooltipWindow.UpdateLines(lines);
            _lastTooltipContent = currentContent;
        }
    }




    private void CheckMouseLeave(object? sender, EventArgs e)
    {
        var currentMousePos = Forms.Control.MousePosition;

        // Only check if mouse position has changed to reduce unnecessary API calls
        if (currentMousePos == _lastMousePosition)
        {
            // Mouse hasn't moved, stop the timer to prevent flickering
            _mouseLeaveCheckTimer?.Stop();
            return;
        }

        _lastMousePosition = currentMousePos;

        if (!IsMouseOverIcon())
        {
            HideTooltipWindow();
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
            if (NotifyIconNative.TryGetNotifyIconRect(_notifyIcon, out var iconRect))
            {
                // Add some padding around the icon for better UX
                var paddedRect = new System.Drawing.Rectangle(
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
                var workingArea = SystemParameters.WorkArea;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                var screenWidth = SystemParameters.PrimaryScreenWidth;

                var trayArea = new System.Drawing.Rectangle(
                    (int)(screenWidth - 100),
                    (int)(workingArea.Bottom),
                    100,
                    (int)(screenHeight - workingArea.Bottom)
                );

                return trayArea.Contains(mousePos);
            }
        }
        catch
        {
            return false;
        }
    }

    private void HideTooltipWindow()
    {
        _tooltipHideTimer?.Stop();
        _mouseLeaveCheckTimer?.Stop();

        if (_tooltipWindow?.ShouldRender == true)
        {
            _tooltipWindow.SetShouldRender(false);
        }
    }

    private void EnsureSystemTooltipSuppressed(bool force = false)
    {
        if (_notifyIcon == null)
        {
            return;
        }

        if (!force && _systemTooltipSuppressed)
        {
            return;
        }

        if (!NotifyIconNative.TryBuildData(_notifyIcon, out var data))
        {
            return;
        }

        // Explicitly exclude NIF_TIP flag to completely remove tooltip functionality
        // Only set MESSAGE and ICON flags to maintain tray icon without tooltip
        data.uFlags = NotifyIconNative.NIF.MESSAGE | NotifyIconNative.NIF.ICON;
        data.uCallbackMessage = NotifyIconNative.TrayMouseMessage;
        data.hIcon = _notifyIcon.Icon?.Handle ?? IntPtr.Zero;
        data.szTip = string.Empty;
        data.szInfo = string.Empty;
        data.szInfoTitle = string.Empty;

        if (NotifyIconNative.ShellNotifyIcon(NotifyIconNative.NIM.MODIFY, ref data))
        {
            _systemTooltipSuppressed = true;
        }
    }

    public void Dispose()
    {
        _monitoringService.SnapshotAvailable -= MonitoringServiceOnSnapshotAvailable;
        _monitoringService.SleepTriggered -= MonitoringServiceOnSleepTriggered;

        if (_notifyIcon != null)
        {
            _notifyIcon.MouseMove -= NotifyIconOnMouseMove;
            _notifyIcon.MouseDown -= NotifyIconOnMouseDown;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        HideTooltipWindow();
        _tooltipWindow?.Close();
        _tooltipWindow = null;
        _tooltipHideTimer?.Stop();
        _tooltipHideTimer = null;
        _mouseLeaveCheckTimer?.Stop();
        _mouseLeaveCheckTimer = null;

        _iconResource?.Dispose();
        _iconResource = null;
    }

    private static class NotifyIconNative
    {
        private static readonly FieldInfo? IdField = typeof(Forms.NotifyIcon).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? WindowField = typeof(Forms.NotifyIcon).GetField("window", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo? CreateHandleMethod = typeof(Forms.NotifyIcon).GetMethod("CreateHandle", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly uint TrayMouseMessage = Convert.ToUInt32(typeof(Forms.NotifyIcon)
            .GetField("WM_TRAYMOUSEMESSAGE", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) ?? 0x800);

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

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
        internal static extern bool ShellNotifyIcon(NIM message, ref NOTIFYICONDATA data);

        [DllImport("shell32.dll")]
        internal static extern IntPtr Shell_NotifyIconGetRect([In] ref NOTIFYICONIDENTIFIER identifier, [Out] out RECT iconLocation);

        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uID;
            public NIF uFlags;
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

        [Flags]
        internal enum NIF : uint
        {
            MESSAGE = 0x00000001,
            ICON = 0x00000002,
            TIP = 0x00000004,
            STATE = 0x00000008,
            INFO = 0x00000010,
            GUID = 0x00000020
        }

        internal enum NIM : uint
        {
            ADD = 0x00000000,
            MODIFY = 0x00000001,
            DELETE = 0x00000002,
            SETFOCUS = 0x00000003,
            SETVERSION = 0x00000004
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

        internal static bool TryGetNotifyIconRect(Forms.NotifyIcon icon, out System.Drawing.Rectangle rect)
        {
            rect = System.Drawing.Rectangle.Empty;

            try
            {
                if (!TryBuildData(icon, out var data))
                    return false;

                var identifier = new NOTIFYICONIDENTIFIER
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
                    hWnd = data.hWnd,
                    uID = data.uID,
                    guidItem = data.guidItem
                };

                var result = Shell_NotifyIconGetRect(ref identifier, out RECT iconRect);
                if (result == IntPtr.Zero) // S_OK
                {
                    rect = new System.Drawing.Rectangle(iconRect.Left, iconRect.Top, iconRect.Width, iconRect.Height);
                    return true;
                }
            }
            catch
            {
                // Fall back to detection method
            }

            return false;
        }
    }

}





