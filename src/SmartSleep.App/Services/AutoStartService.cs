using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace SmartSleep.App.Services;

public class AutoStartService
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private readonly string _appName;
    private readonly string _executablePath;

    public AutoStartService(string? appName = null)
    {
        _appName = appName ?? "SmartSleep";
        _executablePath = GetExecutablePath();
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(_appName) as string;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalizedStored = NormalizePath(value);
        var normalizedCurrent = NormalizePath(_executablePath);
        return string.Equals(normalizedStored, normalizedCurrent, StringComparison.OrdinalIgnoreCase);
    }

    public bool TrySetAutoStart(bool enable, out string? errorMessage)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true) ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key == null)
            {
                errorMessage = "레지스트리 키를 열 수 없습니다.";
                return false;
            }

            if (!enable)
            {
                key.DeleteValue(_appName, false);
            }
            else
            {
                key.SetValue(_appName, $"\"{_executablePath}\"");
            }

            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static string GetExecutablePath()
    {
        var processPath = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmartSleep.App.exe");
    }

    private static string NormalizePath(string path)
    {
        var trimmed = path.Trim().Trim('\"');
        return Path.GetFullPath(trimmed);
    }
}
