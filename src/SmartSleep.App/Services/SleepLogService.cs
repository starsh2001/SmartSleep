using System.Globalization;
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
                var statusToken = entry.WasSuccessful ? "Success" : "Failure";
                var errorToken = entry.ErrorMessage?.Replace("|", "/", StringComparison.OrdinalIgnoreCase) ?? string.Empty;
                var logLine = string.Join('|',
                    entry.Timestamp.ToString("o", CultureInfo.InvariantCulture),
                    entry.PowerAction,
                    statusToken,
                    errorToken);
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
            var parts = line.Split('|');
            if (parts.Length >= 4)
            {
                // Try new pipe-delimited format first
                if (TryParseNewFormat(parts, out entry))
                {
                    return true;
                }
            }

            // Fall back to trying legacy format parsing
            return TryParseLegacyFormat(line, out entry);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseNewFormat(string[] parts, out SleepLogEntry entry)
    {
        entry = null!;

        try
        {
            if (!DateTime.TryParseExact(parts[0], "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var timestamp))
            {
                return false;
            }

            if (!Enum.TryParse(parts[1], out PowerAction action))
            {
                return false;
            }

            var statusToken = parts[2];
            bool wasSuccessful = statusToken.Equals("Success", StringComparison.OrdinalIgnoreCase);
            if (!wasSuccessful && !statusToken.Equals("Failure", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var errorMessage = string.IsNullOrWhiteSpace(parts[3]) ? null : parts[3];

            entry = new SleepLogEntry(timestamp, action, wasSuccessful, errorMessage);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseLegacyFormat(string line, out SleepLogEntry entry)
    {
        entry = null!;

        try
        {
            // Try to parse legacy Korean format logs
            // This is a best-effort attempt to maintain compatibility
            // Legacy format was more free-form, so this is limited

            // Look for common patterns like "2024-01-01 12:00:00 - 절전 성공"
            var parts = line.Split(" - ");
            if (parts.Length >= 2)
            {
                if (DateTime.TryParse(parts[0], out var timestamp))
                {
                    var actionAndStatus = parts[1];

                    PowerAction action = PowerAction.Sleep;
                    bool wasSuccessful = true;
                    string? errorMessage = null;

                    // Simple heuristic parsing for legacy entries
                    if (actionAndStatus.Contains("시스템 종료") || actionAndStatus.Contains("Shutdown"))
                    {
                        action = PowerAction.Shutdown;
                    }

                    if (actionAndStatus.Contains("실패") || actionAndStatus.Contains("failed") || actionAndStatus.Contains("Failed"))
                    {
                        wasSuccessful = false;
                        // Extract error message if possible
                        var failureParts = actionAndStatus.Split('(');
                        if (failureParts.Length > 1)
                        {
                            errorMessage = failureParts[1].TrimEnd(')');
                        }
                    }

                    entry = new SleepLogEntry(timestamp, action, wasSuccessful, errorMessage);
                    return true;
                }
            }

            return false;
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
