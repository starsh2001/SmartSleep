using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SmartSleep.App.Interop;

namespace SmartSleep.App.Utilities;

public class GamepadConnectionEventArgs : EventArgs
{
    public bool IsConnected { get; }
    public string DeviceName { get; }
    public int TotalConnectedCount { get; }

    public GamepadConnectionEventArgs(bool isConnected, string deviceName, int totalConnectedCount)
    {
        IsConnected = isConnected;
        DeviceName = deviceName;
        TotalConnectedCount = totalConnectedCount;
    }
}

public class GamepadActivityReader : IDisposable
{
    private readonly object _lock = new();
    private DateTime _lastGamepadInputTime = DateTime.MinValue;
    private IntPtr _hwnd;
    private HwndSource? _hwndSource;
    private bool _isRegistered = false;
    private bool _disposed = false;
    private IntPtr _deviceNotificationHandle = IntPtr.Zero;
    private int _connectedGamepadCount = 0;

    public event EventHandler<GamepadConnectionEventArgs>? GamepadConnectionChanged;

    public DateTime LastGamepadInputTime
    {
        get
        {
            lock (_lock)
            {
                return _lastGamepadInputTime;
            }
        }
    }

    public TimeSpan GetGamepadIdleTime()
    {
        lock (_lock)
        {
            if (_lastGamepadInputTime == DateTime.MinValue)
            {
                return TimeSpan.MaxValue;
            }
            return DateTime.Now - _lastGamepadInputTime;
        }
    }

    public bool Initialize()
    {
        try
        {
            // Clean up any previous registration attempts first
            CleanupPreviousRegistrations();

            // Create a hidden window to receive raw input messages
            var window = new Window
            {
                Width = 0,
                Height = 0,
                Left = -1000,
                Top = -1000,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Visibility = Visibility.Hidden
            };
            window.Show();

            _hwndSource = PresentationSource.FromVisual(window) as HwndSource;
            if (_hwndSource == null)
                return false;

            _hwnd = _hwndSource.Handle;
            _hwndSource.AddHook(WndProc);

            // Register for gamepad and joystick raw input
            var devices = new NativeMethods.RAWINPUTDEVICE[]
            {
                new()
                {
                    usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                    usUsage = NativeMethods.HID_USAGE_GENERIC_GAMEPAD,
                    dwFlags = NativeMethods.RIDEV_INPUTSINK,
                    hwndTarget = _hwnd
                },
                new()
                {
                    usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                    usUsage = NativeMethods.HID_USAGE_GENERIC_JOYSTICK,
                    dwFlags = NativeMethods.RIDEV_INPUTSINK,
                    hwndTarget = _hwnd
                }
            };

            var size = (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>();

            // Try registration with retry logic
            _isRegistered = TryRegisterRawInputDevices(devices, size);

            if (_isRegistered)
            {
                // Register for device change notifications
                RegisterDeviceChangeNotification();

                lock (_lock)
                {
                    _lastGamepadInputTime = DateTime.Now;
                }
            }
            else
            {
                // If registration failed, clean up the window
                _hwndSource?.RemoveHook(WndProc);
                _hwndSource?.Dispose();
                _hwndSource = null;
                _hwnd = IntPtr.Zero;
            }

            return _isRegistered;
        }
        catch
        {
            return false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_INPUT)
        {
            ProcessRawInput(lParam);
        }
        else if (msg == NativeMethods.WM_DEVICECHANGE)
        {
            ProcessDeviceChange(wParam, lParam);
        }
        return IntPtr.Zero;
    }

    private void ProcessRawInput(IntPtr lParam)
    {
        try
        {
            uint dwSize = 0;
            NativeMethods.GetRawInputData(lParam, 0x10000003, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());

            if (dwSize > 0)
            {
                var buffer = Marshal.AllocHGlobal((int)dwSize);
                try
                {
                    var result = NativeMethods.GetRawInputData(lParam, 0x10000003, buffer, ref dwSize, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());

                    if (result > 0)
                    {
                        var header = Marshal.PtrToStructure<NativeMethods.RAWINPUTHEADER>(buffer);

                        // Check if it's HID input (gamepad/joystick)
                        if (header.dwType == NativeMethods.RIM_TYPEHID)
                        {
                            lock (_lock)
                            {
                                _lastGamepadInputTime = DateTime.Now;
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
        catch
        {
            // Ignore errors in raw input processing
        }
    }

    private void RegisterDeviceChangeNotification()
    {
        try
        {
            var notificationFilter = new NativeMethods.DEV_BROADCAST_DEVICEINTERFACE
            {
                dbcc_size = (uint)Marshal.SizeOf<NativeMethods.DEV_BROADCAST_DEVICEINTERFACE>(),
                dbcc_devicetype = NativeMethods.DBT_DEVTYP_DEVICEINTERFACE,
                dbcc_classguid = NativeMethods.GUID_DEVINTERFACE_HID
            };

            var filterPtr = Marshal.AllocHGlobal(Marshal.SizeOf(notificationFilter));
            try
            {
                Marshal.StructureToPtr(notificationFilter, filterPtr, false);
                _deviceNotificationHandle = NativeMethods.RegisterDeviceNotification(
                    _hwnd, filterPtr, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            }
            finally
            {
                Marshal.FreeHGlobal(filterPtr);
            }
        }
        catch
        {
            // Ignore registration errors
        }
    }

    private void ProcessDeviceChange(IntPtr wParam, IntPtr lParam)
    {
        try
        {
            var eventType = (uint)wParam.ToInt32();

            if (eventType == NativeMethods.DBT_DEVICEARRIVAL || eventType == NativeMethods.DBT_DEVICEREMOVECOMPLETE)
            {
                if (lParam != IntPtr.Zero)
                {
                    var header = Marshal.PtrToStructure<NativeMethods.DEV_BROADCAST_HDR>(lParam);

                    if (header.dbch_devicetype == NativeMethods.DBT_DEVTYP_DEVICEINTERFACE)
                    {
                        var deviceInterface = Marshal.PtrToStructure<NativeMethods.DEV_BROADCAST_DEVICEINTERFACE>(lParam);

                        if (deviceInterface.dbcc_classguid == NativeMethods.GUID_DEVINTERFACE_HID)
                        {
                            var isConnected = eventType == NativeMethods.DBT_DEVICEARRIVAL;

                            lock (_lock)
                            {
                                if (isConnected)
                                {
                                    _connectedGamepadCount++;
                                    _lastGamepadInputTime = DateTime.Now; // Reset idle time when gamepad connects
                                }
                                else
                                {
                                    _connectedGamepadCount = Math.Max(0, _connectedGamepadCount - 1);
                                }
                            }

                            // Extract device name from the path
                            var deviceName = ExtractDeviceName(deviceInterface.dbcc_name);

                            // Fire the event
                            GamepadConnectionChanged?.Invoke(this,
                                new GamepadConnectionEventArgs(isConnected, deviceName, _connectedGamepadCount));
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore device change processing errors
        }
    }

    private static string ExtractDeviceName(string devicePath)
    {
        try
        {
            // Extract meaningful name from device path
            if (string.IsNullOrEmpty(devicePath))
                return "Unknown Gamepad";

            // Look for common gamepad identifiers
            var path = devicePath.ToUpperInvariant();

            if (path.Contains("XBOX"))
                return "Xbox Controller";
            else if (path.Contains("PS4") || path.Contains("DUALSHOCK"))
                return "PS4 Controller";
            else if (path.Contains("PS5") || path.Contains("DUALSENSE"))
                return "PS5 Controller";
            else if (path.Contains("SWITCH"))
                return "Switch Controller";
            else if (path.Contains("JOY"))
                return "Joystick";
            else
                return "Gamepad";
        }
        catch
        {
            return "Unknown Gamepad";
        }
    }

    private void CleanupPreviousRegistrations()
    {
        try
        {
            // Attempt to unregister any previous raw input devices
            // This helps clean up if a previous instance didn't shut down properly
            var cleanupDevices = new NativeMethods.RAWINPUTDEVICE[]
            {
                new()
                {
                    usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                    usUsage = NativeMethods.HID_USAGE_GENERIC_GAMEPAD,
                    dwFlags = 0x00000001, // RIDEV_REMOVE
                    hwndTarget = IntPtr.Zero
                },
                new()
                {
                    usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                    usUsage = NativeMethods.HID_USAGE_GENERIC_JOYSTICK,
                    dwFlags = 0x00000001, // RIDEV_REMOVE
                    hwndTarget = IntPtr.Zero
                }
            };

            var size = (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>();
            NativeMethods.RegisterRawInputDevices(cleanupDevices, (uint)cleanupDevices.Length, size);
            // Don't care about the result - this is just cleanup
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private bool TryRegisterRawInputDevices(NativeMethods.RAWINPUTDEVICE[] devices, uint size)
    {
        // Try registration up to 3 times with small delays
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (NativeMethods.RegisterRawInputDevices(devices, (uint)devices.Length, size))
                {
                    return true;
                }

                // Small delay before retry
                if (attempt < 2)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch
            {
                if (attempt < 2)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        return false;
    }

    public int GetConnectedGamepadCount()
    {
        lock (_lock)
        {
            return _connectedGamepadCount;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Unregister device change notification
        if (_deviceNotificationHandle != IntPtr.Zero)
        {
            try
            {
                NativeMethods.UnregisterDeviceNotification(_deviceNotificationHandle);
                _deviceNotificationHandle = IntPtr.Zero;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Always try to unregister raw input devices, even if we think they weren't registered
        // This provides extra safety in case of inconsistent state
        try
        {
            // Unregister raw input devices
            var devices = new NativeMethods.RAWINPUTDEVICE[]
            {
                    new()
                    {
                        usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                        usUsage = NativeMethods.HID_USAGE_GENERIC_GAMEPAD,
                        dwFlags = 0x00000001, // RIDEV_REMOVE
                        hwndTarget = IntPtr.Zero
                    },
                    new()
                    {
                        usUsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                        usUsage = NativeMethods.HID_USAGE_GENERIC_JOYSTICK,
                        dwFlags = 0x00000001, // RIDEV_REMOVE
                        hwndTarget = IntPtr.Zero
                    }
            };

            var size = (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>();
            NativeMethods.RegisterRawInputDevices(devices, (uint)devices.Length, size);
        }
        catch
        {
            // Ignore cleanup errors - this is expected in some cases
        }

        // Clean up window resources
        try
        {
            _hwndSource?.RemoveHook(WndProc);
        }
        catch
        {
            // Ignore hook removal errors
        }

        try
        {
            _hwndSource?.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }

        _hwndSource = null;
        _hwnd = IntPtr.Zero;
        _isRegistered = false;
    }
}