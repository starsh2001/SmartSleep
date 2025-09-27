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
            PowerAction.Sleep => "절전",
            PowerAction.Shutdown => "시스템 종료",
            _ => "전원"
        };

        var statusText = WasSuccessful ? "성공" : $"실패 ({ErrorMessage})";
        return $"{Timestamp:yyyy-MM-dd HH:mm:ss} - {actionText} {statusText}";
    }
}