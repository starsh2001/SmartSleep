using System;
using System.Windows.Media;

namespace SmartSleep.App.Utilities;

public static class StatusDisplayHelper
{
    private static readonly System.Windows.Media.Brush DefaultBrush = System.Windows.Media.Brushes.LightGray;
    private static readonly System.Windows.Media.Brush SuccessBrush = System.Windows.Media.Brushes.MediumSpringGreen;
    private static readonly System.Windows.Media.Brush WarningBrush = System.Windows.Media.Brushes.Orange;
    private static readonly System.Windows.Media.Brush IdleBrush = System.Windows.Media.Brushes.SlateGray;

    public static (string Text, System.Windows.Media.Brush Brush) FormatStatus(string? statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return (string.Empty, IdleBrush);
        }

        var trimmed = statusMessage.Trim();
        var brush = DetermineBrush(trimmed);
        var display = LocalizationManager.Format("Status_DisplayPrefix", trimmed);
        return (display, brush);
    }

    private static System.Windows.Media.Brush DetermineBrush(string message)
    {
        var sleepKeyword = LocalizationManager.GetString("StatusKeyword_Sleep");
        var requestKeyword = LocalizationManager.GetString("StatusKeyword_Request");
        if (message.Contains(sleepKeyword, StringComparison.OrdinalIgnoreCase) ||
            message.Contains(requestKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return SuccessBrush;
        }

        var cpuKeyword = LocalizationManager.GetString("StatusKeyword_CPU");
        var networkKeyword = LocalizationManager.GetString("StatusKeyword_Network");
        if (message.Contains(cpuKeyword, StringComparison.OrdinalIgnoreCase) ||
            message.Contains(networkKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return WarningBrush;
        }

        return DefaultBrush;
    }
}
