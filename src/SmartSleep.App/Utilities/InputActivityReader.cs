using System;
using System.Runtime.InteropServices;
using SmartSleep.App.Interop;

namespace SmartSleep.App.Utilities;

public static class InputActivityReader
{
    public static TimeSpan GetIdleTime()
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
}
