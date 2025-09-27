using System;

namespace SmartSleep.App.Models;

public class IdleSettings
{
    public bool UseInputActivity { get; set; } = true;
    public bool IncludeGamepadInput { get; set; } = true;
    public int InputIdleThresholdSeconds { get; set; } = 1200;
    public bool UseCpuActivity { get; set; } = true;
    public double CpuUsagePercentageThreshold { get; set; } = 10.0;
    public int CpuIdleDurationSeconds { get; set; } = 600;
    public int CpuSmoothingWindow { get; set; } = 5;
    public bool UseNetworkActivity { get; set; } = true;
    public double NetworkKilobytesPerSecondThreshold { get; set; } = 128.0;
    public int NetworkIdleDurationSeconds { get; set; } = 600;
    public int NetworkSmoothingWindow { get; set; } = 5;

    public TimeSpan InputIdleThreshold => TimeSpan.FromSeconds(Math.Max(0, InputIdleThresholdSeconds));
    public TimeSpan CpuIdleDurationRequirement => TimeSpan.FromSeconds(Math.Max(0, CpuIdleDurationSeconds));
    public TimeSpan NetworkIdleDurationRequirement => TimeSpan.FromSeconds(Math.Max(0, NetworkIdleDurationSeconds));

    public static IdleSettings CreateDefault() => new();

    public IdleSettings Clone() => new()
    {
        UseInputActivity = UseInputActivity,
        IncludeGamepadInput = IncludeGamepadInput,
        InputIdleThresholdSeconds = InputIdleThresholdSeconds,
        UseCpuActivity = UseCpuActivity,
        CpuUsagePercentageThreshold = CpuUsagePercentageThreshold,
        CpuIdleDurationSeconds = CpuIdleDurationSeconds,
        CpuSmoothingWindow = CpuSmoothingWindow,
        UseNetworkActivity = UseNetworkActivity,
        NetworkKilobytesPerSecondThreshold = NetworkKilobytesPerSecondThreshold,
        NetworkIdleDurationSeconds = NetworkIdleDurationSeconds,
        NetworkSmoothingWindow = NetworkSmoothingWindow,
    };
}
