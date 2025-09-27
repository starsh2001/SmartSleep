using System;
using System.Runtime.InteropServices;
using SmartSleep.App.Interop;

namespace SmartSleep.App.Utilities;

public static class InputActivityReader
{
    private static GamepadActivityReader? _gamepadReader;
    private static readonly object _lock = new();
    private static bool _gamepadInitialized = false;
    private static TimeSpan _cachedGamepadIdleTime = TimeSpan.MaxValue;
    private static Task? _gamepadInitializationTask;
    private static System.Threading.Timer? _inputMonitoringTimer;
    private static TimeSpan _lastIdleTime = TimeSpan.Zero;

    public static event EventHandler<GamepadConnectionEventArgs>? GamepadConnectionChanged;
    public static event EventHandler? InputActivityDetected;

    public static TimeSpan GetIdleTime()
    {
        return GetIdleTime(includeGamepad: false);
    }

    public static TimeSpan GetIdleTime(bool includeGamepad)
    {
        // Get keyboard/mouse idle time
        var keyboardMouseIdle = GetKeyboardMouseIdleTime();

        if (!includeGamepad)
        {
            return keyboardMouseIdle;
        }

        // Get gamepad idle time
        var gamepadIdle = GetGamepadIdleTime();

        // Return the minimum (most recent activity)
        return keyboardMouseIdle < gamepadIdle ? keyboardMouseIdle : gamepadIdle;
    }

    private static TimeSpan GetKeyboardMouseIdleTime()
    {
        var lastInputInfo = new NativeMethods.LASTINPUTINFO
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.LASTINPUTINFO>()
        };

        if (!NativeMethods.GetLastInputInfo(ref lastInputInfo))
        {
            return TimeSpan.Zero;
        }

        var tickCount = unchecked((uint)Environment.TickCount);
        var lastInputTick = lastInputInfo.dwTime;
        var elapsed = unchecked(tickCount - lastInputTick);
        return TimeSpan.FromMilliseconds(elapsed);
    }

    private static TimeSpan GetGamepadIdleTime()
    {
        // Start background initialization if not already started
        if (_gamepadInitializationTask == null)
        {
            lock (_lock)
            {
                if (_gamepadInitializationTask == null)
                {
                    _gamepadInitializationTask = Task.Run(InitializeGamepadInBackground);
                }
            }
        }

        // Return cached value immediately (no blocking)
        lock (_lock)
        {
            return _cachedGamepadIdleTime;
        }
    }

    private static async Task InitializeGamepadInBackground()
    {
        try
        {
            await Task.Delay(100); // Small delay to avoid blocking main thread startup

            var reader = new GamepadActivityReader();
            reader.GamepadConnectionChanged += OnGamepadConnectionChanged;

            // Try initialization in background
            bool initialized = await Task.Run(() => TryInitializeGamepadReader(reader));

            lock (_lock)
            {
                if (initialized)
                {
                    _gamepadReader = reader;
                    _gamepadInitialized = true;

                    // Start periodic update in background
                    _ = Task.Run(UpdateGamepadIdleTimeLoop);
                }
                else
                {
                    reader.Dispose();
                    // Keep _cachedGamepadIdleTime as MaxValue
                }
            }
        }
        catch
        {
            // Initialization failed - keep cached value as MaxValue
        }
    }

    private static async Task UpdateGamepadIdleTimeLoop()
    {
        while (true)
        {
            try
            {
                await Task.Delay(100); // Update every 100ms for responsive gamepad detection

                lock (_lock)
                {
                    if (_gamepadReader != null)
                    {
                        var gamepadCount = _gamepadReader.GetConnectedGamepadCount();
                        if (gamepadCount > 0)
                        {
                            _cachedGamepadIdleTime = _gamepadReader.GetGamepadIdleTime();
                        }
                        else
                        {
                            _cachedGamepadIdleTime = TimeSpan.MaxValue;
                        }
                    }
                    else
                    {
                        // Reader disposed - exit loop
                        break;
                    }
                }
            }
            catch
            {
                // Continue on errors
            }
        }
    }


    private static bool TryInitializeGamepadReader(GamepadActivityReader reader)
    {
        // Try initialization up to 2 times
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                if (reader.Initialize())
                {
                    return true;
                }

                // If first attempt failed, small delay before retry
                if (attempt == 0)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
            catch
            {
                // If exception occurred, small delay before retry
                if (attempt == 0)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
        }

        return false;
    }

    private static void OnGamepadConnectionChanged(object? sender, GamepadConnectionEventArgs e)
    {
        GamepadConnectionChanged?.Invoke(null, e);
    }

    public static int GetConnectedGamepadCount()
    {
        lock (_lock)
        {
            return _gamepadReader?.GetConnectedGamepadCount() ?? 0;
        }
    }

    public static void StartInputMonitoring()
    {
        lock (_lock)
        {
            if (_inputMonitoringTimer == null)
            {
                _lastIdleTime = GetKeyboardMouseIdleTime();
                _inputMonitoringTimer = new System.Threading.Timer(MonitorInputActivity, null,
                    TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
            }
        }
    }

    public static void StopInputMonitoring()
    {
        lock (_lock)
        {
            _inputMonitoringTimer?.Dispose();
            _inputMonitoringTimer = null;
        }
    }

    private static void MonitorInputActivity(object? state)
    {
        try
        {
            var currentIdleTime = GetKeyboardMouseIdleTime();

            lock (_lock)
            {
                // If idle time decreased, input activity occurred
                if (currentIdleTime < _lastIdleTime)
                {
                    // Fire event on main thread
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        InputActivityDetected?.Invoke(null, EventArgs.Empty);
                    });
                }
                _lastIdleTime = currentIdleTime;
            }
        }
        catch
        {
            // Ignore errors in monitoring
        }
    }

    public static void Cleanup()
    {
        lock (_lock)
        {
            StopInputMonitoring();

            if (_gamepadReader != null)
            {
                _gamepadReader.GamepadConnectionChanged -= OnGamepadConnectionChanged;
                _gamepadReader.Dispose();
                _gamepadReader = null;
            }

            _gamepadInitialized = false;
        }
    }
}
