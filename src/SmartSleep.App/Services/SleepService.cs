using System.Runtime.InteropServices;
using SmartSleep.App.Interop;
using SmartSleep.App.Models;

namespace SmartSleep.App.Services;

public class SleepService
{
    public bool TryExecutePowerAction(PowerAction action, out int errorCode)
    {
        return action switch
        {
            PowerAction.Sleep => TryEnterSleep(out errorCode),
            PowerAction.Shutdown => TryShutdown(out errorCode),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
    }

    public bool TryEnterSleep(out int errorCode)
    {
        var result = NativeMethods.SetSuspendState(false, false, false);
        errorCode = result ? 0 : Marshal.GetLastWin32Error();
        return result;
    }

    public bool TryShutdown(out int errorCode)
    {
        var result = NativeMethods.ExitWindowsEx(NativeMethods.EWX_SHUTDOWN, 0);
        errorCode = result ? 0 : Marshal.GetLastWin32Error();
        return result;
    }
}
