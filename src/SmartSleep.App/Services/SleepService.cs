using System.Runtime.InteropServices;
using SmartSleep.App.Interop;

namespace SmartSleep.App.Services;

public class SleepService
{
    public bool TryEnterSleep(out int errorCode)
    {
        var result = NativeMethods.SetSuspendState(false, false, false);
        errorCode = result ? 0 : Marshal.GetLastWin32Error();
        return result;
    }
}
