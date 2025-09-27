using System;
using System.Runtime.InteropServices;
using SmartSleep.App.Interop;

namespace SmartSleep.App.Utilities;

public static class InputActivityReader
{
    private static GamepadActivityReader? _gamepadReader;
    private static readonly object _lock = new();

    public static event EventHandler<GamepadConnectionEventArgs>? GamepadConnectionChanged;

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
        lock (_lock)
        {
            if (_gamepadReader == null)
            {
                _gamepadReader = new GamepadActivityReader();
                _gamepadReader.GamepadConnectionChanged += OnGamepadConnectionChanged;

                // Try initialization with retry logic
                if (!TryInitializeGamepadReader())
                {
                    return TimeSpan.MaxValue;
                }
            }

            return _gamepadReader.GetGamepadIdleTime();
        }
    }

    private static bool TryInitializeGamepadReader()
    {
        // Try initialization up to 2 times
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                if (_gamepadReader?.Initialize() == true)
                {
                    return true;
                }

                // If first attempt failed, dispose and recreate
                if (attempt == 0)
                {
                    _gamepadReader?.Dispose();
                    _gamepadReader = new GamepadActivityReader();
                    _gamepadReader.GamepadConnectionChanged += OnGamepadConnectionChanged;
                    System.Threading.Thread.Sleep(200); // Brief delay before retry
                }
            }
            catch
            {
                // If exception occurred, clean up and try again
                if (attempt == 0)
                {
                    try
                    {
                        _gamepadReader?.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                    _gamepadReader = new GamepadActivityReader();
                    _gamepadReader.GamepadConnectionChanged += OnGamepadConnectionChanged;
                }
            }
        }

        // All attempts failed, clean up
        try
        {
            _gamepadReader?.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }
        _gamepadReader = null;
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

    public static void Cleanup()
    {
        lock (_lock)
        {
            if (_gamepadReader != null)
            {
                _gamepadReader.GamepadConnectionChanged -= OnGamepadConnectionChanged;
                _gamepadReader.Dispose();
                _gamepadReader = null;
            }
        }
    }
}
