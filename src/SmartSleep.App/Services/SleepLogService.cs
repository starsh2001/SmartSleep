using System.IO;
using System.Text;
using SmartSleep.App.Models;

namespace SmartSleep.App.Services;

public class SleepLogService
{
    private readonly object _lock = new();
    private readonly string _logFilePath;

    public SleepLogService()
    {
        // Log file in the same directory as the executable
        var exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
        _logFilePath = Path.Combine(exeDirectory, "SmartSleep_Log.txt");
    }

    public void LogSleepAction(PowerAction powerAction, bool wasSuccessful, string? errorMessage = null)
    {
        var logEntry = new SleepLogEntry(DateTime.Now, powerAction, wasSuccessful, errorMessage);
        WriteLogEntry(logEntry);
    }

    private void WriteLogEntry(SleepLogEntry entry)
    {
        lock (_lock)
        {
            try
            {
                var logLine = entry.ToString();
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Ignore logging errors - we don't want logging failures to affect the main functionality
            }
        }
    }

    public List<SleepLogEntry> GetRecentEntries(int maxEntries = 100)
    {
        lock (_lock)
        {
            var entries = new List<SleepLogEntry>();

            try
            {
                if (!File.Exists(_logFilePath))
                    return entries;

                var lines = File.ReadAllLines(_logFilePath, Encoding.UTF8);

                // Take the last maxEntries lines
                var recentLines = lines.TakeLast(maxEntries);

                foreach (var line in recentLines)
                {
                    if (TryParseLogLine(line, out var entry))
                    {
                        entries.Add(entry);
                    }
                }
            }
            catch
            {
                // Return empty list if there's an error reading the log
            }

            return entries;
        }
    }

    private static bool TryParseLogLine(string line, out SleepLogEntry entry)
    {
        entry = null!;

        try
        {
            // Expected format: "2024-01-01 12:00:00 - 절전 성공"
            // or: "2024-01-01 12:00:00 - 시스템 종료 실패 (오류 메시지)"

            if (line.Length < 19) // Minimum length for timestamp
                return false;

            var timestampPart = line[..19]; // "2024-01-01 12:00:00"
            if (!DateTime.TryParseExact(timestampPart, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var timestamp))
                return false;

            var remainingPart = line[19..].Trim();
            if (!remainingPart.StartsWith(" - "))
                return false;

            var actionAndStatus = remainingPart[3..]; // Remove " - "

            PowerAction powerAction;
            bool wasSuccessful;
            string? errorMessage = null;

            if (actionAndStatus.Contains("절전"))
            {
                powerAction = PowerAction.Sleep;
            }
            else if (actionAndStatus.Contains("시스템 종료"))
            {
                powerAction = PowerAction.Shutdown;
            }
            else
            {
                return false; // Unknown action
            }

            if (actionAndStatus.Contains("성공"))
            {
                wasSuccessful = true;
            }
            else if (actionAndStatus.Contains("실패"))
            {
                wasSuccessful = false;

                // Extract error message if present
                var errorStart = actionAndStatus.IndexOf('(');
                var errorEnd = actionAndStatus.LastIndexOf(')');
                if (errorStart >= 0 && errorEnd > errorStart)
                {
                    errorMessage = actionAndStatus.Substring(errorStart + 1, errorEnd - errorStart - 1);
                }
            }
            else
            {
                return false; // Unknown status
            }

            entry = new SleepLogEntry(timestamp, powerAction, wasSuccessful, errorMessage);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetLogFilePath()
    {
        return _logFilePath;
    }

    public bool LogFileExists()
    {
        return File.Exists(_logFilePath);
    }

    public long GetLogFileSize()
    {
        try
        {
            return File.Exists(_logFilePath) ? new FileInfo(_logFilePath).Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    public void ClearLog()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    File.Delete(_logFilePath);
                }
            }
            catch
            {
                // Ignore deletion errors
            }
        }
    }
}