using System.Runtime.InteropServices;
using System.Windows;
using SmartSleep.App.Interop;
using SmartSleep.App.Models;
using SmartSleep.App.ViewModels;
using SmartSleep.App.Views;

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

    public bool TryExecutePowerActionWithConfirmation(PowerAction action, bool showConfirmation, int countdownSeconds, out int errorCode)
    {
        if (showConfirmation)
        {
            bool? dialogResult = null;

            // Ensure dialog runs on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var viewModel = new ConfirmationDialogViewModel(action, countdownSeconds);
                var dialog = new ConfirmationDialog(viewModel);

                dialogResult = dialog.ShowDialog();
                viewModel.StopCountdown();
            });

            if (dialogResult != true)
            {
                errorCode = 0;
                return false;
            }
        }

        return TryExecutePowerAction(action, out errorCode);
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
