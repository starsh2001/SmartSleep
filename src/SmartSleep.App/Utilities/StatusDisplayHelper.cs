using System;
using System.Windows.Media;

namespace SmartSleep.App.Utilities;

public static class StatusDisplayHelper
{
    private static readonly System.Windows.Media.Brush DefaultBrush = System.Windows.Media.Brushes.LightGray;
    private static readonly System.Windows.Media.Brush SuccessBrush = System.Windows.Media.Brushes.MediumSpringGreen;
    private static readonly System.Windows.Media.Brush WarningBrush = System.Windows.Media.Brushes.Orange;
    private static readonly System.Windows.Media.Brush IdleBrush = System.Windows.Media.Brushes.SlateGray;

    public static (string Text, System.Windows.Media.Brush Brush) FormatStatus(string? statusMessage, bool forTooltip = false)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            var idleBrush = forTooltip ? System.Windows.Media.Brushes.White : IdleBrush;
            return (string.Empty, idleBrush);
        }

        var trimmed = statusMessage.Trim();
        var brush = DetermineBrush(trimmed, forTooltip);
        var display = LocalizationManager.Format("Status_DisplayPrefix", trimmed);
        return (display, brush);
    }

    public static System.Windows.Media.Brush GetCpuBrush(double usage, double threshold, bool forTooltip = false)
    {
        var normalBrush = forTooltip ? System.Windows.Media.Brushes.White : DefaultBrush;
        return usage >= threshold ? WarningBrush : normalBrush;
    }

    public static System.Windows.Media.Brush GetNetworkBrush(double usage, double threshold, bool forTooltip = false)
    {
        var normalBrush = forTooltip ? System.Windows.Media.Brushes.White : DefaultBrush;
        return usage >= threshold ? WarningBrush : normalBrush;
    }

    public static (string Text, System.Windows.Media.Brush Brush) GetStatusMessage(
        string? statusMessage,
        bool inputActivityDetected,
        bool cpuExceeding,
        bool networkExceeding,
        bool forTooltip = false)
    {
        // Check if this is an alert message (activity detection or threshold exceeded)
        var activityDetectedMsg = LocalizationManager.GetString("Status_ActivityDetected");
        var cpuExceededMsg = LocalizationManager.GetString("Status_CpuExceeded");
        var networkExceededMsg = LocalizationManager.GetString("Status_NetworkExceeded");

        if (statusMessage == activityDetectedMsg || statusMessage == cpuExceededMsg || statusMessage == networkExceededMsg)
        {
            return (statusMessage, WarningBrush);
        }

        // Normal status - use default formatting
        return FormatStatus(statusMessage, forTooltip);
    }

    private static System.Windows.Media.Brush DetermineBrush(string message, bool forTooltip = false)
    {
        var sleepKeyword = LocalizationManager.GetString("StatusKeyword_Sleep");
        var requestKeyword = LocalizationManager.GetString("StatusKeyword_Request");
        var shutdownKeyword = LocalizationManager.GetString("StatusKeyword_Shutdown");
        if (message.Contains(sleepKeyword, StringComparison.OrdinalIgnoreCase) ||
            message.Contains(requestKeyword, StringComparison.OrdinalIgnoreCase) ||
            message.Contains(shutdownKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return SuccessBrush;
        }

        // Check for CPU keywords (CPU is same in both languages)
        if (message.Contains("CPU", StringComparison.OrdinalIgnoreCase))
        {
            return WarningBrush;
        }

        // Check for Network keywords (both English "Network" and Korean "네트워크")
        if (message.Contains("Network", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("네트워크", StringComparison.OrdinalIgnoreCase))
        {
            return WarningBrush;
        }

        return forTooltip ? System.Windows.Media.Brushes.White : DefaultBrush;
    }
}
