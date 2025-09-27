using System;

namespace SmartSleep.App.Models;

public class IdleSettings
{
    public bool UseInputActivity { get; set; } = true;
    public bool IncludeGamepadInput { get; set; } = true;
    public bool UseCpuActivity { get; set; } = true;
    public double CpuUsagePercentageThreshold { get; set; } = 10.0;
    public int CpuSmoothingWindow { get; set; } = 5;
    public bool UseNetworkActivity { get; set; } = true;
    public double NetworkKilobytesPerSecondThreshold { get; set; } = 128.0;
    public int NetworkSmoothingWindow { get; set; } = 5;

    // Unified idle time for all conditions
    public int IdleTimeSeconds { get; set; } = 1200;

    public TimeSpan IdleTimeThreshold => TimeSpan.FromSeconds(Math.Max(0, IdleTimeSeconds));

    // Properties for all monitoring types to use the same threshold
    public TimeSpan InputIdleThreshold => IdleTimeThreshold;
    public TimeSpan CpuIdleDurationRequirement => IdleTimeThreshold;
    public TimeSpan NetworkIdleDurationRequirement => IdleTimeThreshold;

    public static IdleSettings CreateDefault() => new();

    public IdleSettings Clone() => new()
    {
        UseInputActivity = UseInputActivity,
        IncludeGamepadInput = IncludeGamepadInput,
        UseCpuActivity = UseCpuActivity,
        CpuUsagePercentageThreshold = CpuUsagePercentageThreshold,
        CpuSmoothingWindow = CpuSmoothingWindow,
        UseNetworkActivity = UseNetworkActivity,
        NetworkKilobytesPerSecondThreshold = NetworkKilobytesPerSecondThreshold,
        NetworkSmoothingWindow = NetworkSmoothingWindow,
        IdleTimeSeconds = IdleTimeSeconds,
    };
}
