using SmartSleep.App.Utilities;

namespace SmartSleep.App.Models;

public class SleepLogEntry
{
    public DateTime Timestamp { get; set; }
    public PowerAction PowerAction { get; set; }
    public bool WasSuccessful { get; set; }
    public string? ErrorMessage { get; set; }

    public SleepLogEntry(DateTime timestamp, PowerAction powerAction, bool wasSuccessful, string? errorMessage = null)
    {
        Timestamp = timestamp;
        PowerAction = powerAction;
        WasSuccessful = wasSuccessful;
        ErrorMessage = errorMessage;
    }

    public override string ToString()
    {
        var actionText = PowerAction switch
        {
            PowerAction.Sleep => LocalizationManager.GetString("SleepLog_Action_Sleep"),
            PowerAction.Shutdown => LocalizationManager.GetString("SleepLog_Action_Shutdown"),
            _ => LocalizationManager.GetString("SleepLog_Action_Power")
        };

        string statusText;
        if (WasSuccessful)
        {
            statusText = LocalizationManager.GetString("SleepLog_Status_Success");
        }
        else if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            statusText = LocalizationManager.Format("SleepLog_Status_Failure", ErrorMessage);
        }
        else
        {
            statusText = LocalizationManager.GetString("SleepLog_Status_FailureNoReason");
        }

        return LocalizationManager.Format("SleepLog_Format", Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), actionText, statusText);
    }
}
