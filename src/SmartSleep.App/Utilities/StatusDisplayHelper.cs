using System;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace SmartSleep.App.Utilities;

public static class StatusDisplayHelper
{
    private static readonly Brush DefaultBrush = Brushes.LightGray;
    private static readonly Brush SuccessBrush = Brushes.MediumSpringGreen;
    private static readonly Brush WarningBrush = Brushes.Orange;
    private static readonly Brush IdleBrush = Brushes.SlateGray;

    public static (string Text, Brush Brush) FormatStatus(string? statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return (string.Empty, IdleBrush);
        }

        var trimmed = statusMessage.Trim();
        var brush = DetermineBrush(trimmed);
        return ($"[Stat] {trimmed}", brush);
    }

    private static Brush DetermineBrush(string message)
    {
        if (message.Contains("예상 절전", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("절전 요청", StringComparison.OrdinalIgnoreCase))
        {
            return SuccessBrush;
        }

        if (message.Contains("CPU 사용량", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("네트워크", StringComparison.OrdinalIgnoreCase))
        {
            return WarningBrush;
        }

        return DefaultBrush;
    }
}
