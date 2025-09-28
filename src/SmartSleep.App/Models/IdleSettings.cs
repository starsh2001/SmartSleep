using System;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Models;

public class IdleSettings
{
    public bool UseInputActivity { get; set; } = DefaultValues.UseInputActivity;
    public bool IncludeGamepadInput { get; set; } = DefaultValues.IncludeGamepadInput;
    public bool UseCpuActivity { get; set; } = DefaultValues.UseCpuActivity;
    public double CpuUsagePercentageThreshold { get; set; } = DefaultValues.CpuUsagePercentageThreshold;
    public int CpuSmoothingWindow { get; set; } = DefaultValues.CpuSmoothingWindow;
    public bool UseNetworkActivity { get; set; } = DefaultValues.UseNetworkActivity;
    public double NetworkKilobitsPerSecondThreshold { get; set; } = DefaultValues.NetworkKilobitsPerSecondThreshold;
    public int NetworkSmoothingWindow { get; set; } = DefaultValues.NetworkSmoothingWindow;

    // Unified idle time for all conditions
    public int IdleTimeSeconds { get; set; } = DefaultValues.IdleTimeSeconds;

    public TimeSpan IdleTimeThreshold => TimeSpan.FromSeconds(Math.Max(0, IdleTimeSeconds));

    // Properties for all monitoring types to use the same threshold
    public TimeSpan InputIdleThreshold => IdleTimeThreshold;
    public TimeSpan CpuIdleDurationRequirement => IdleTimeThreshold;
    public TimeSpan NetworkIdleDurationRequirement => IdleTimeThreshold;

    public static IdleSettings CreateDefault() => DefaultValues.CreateDefaultIdleSettings();

    public IdleSettings Clone() => new()
    {
        UseInputActivity = UseInputActivity,
        IncludeGamepadInput = IncludeGamepadInput,
        UseCpuActivity = UseCpuActivity,
        CpuUsagePercentageThreshold = CpuUsagePercentageThreshold,
        CpuSmoothingWindow = CpuSmoothingWindow,
        UseNetworkActivity = UseNetworkActivity,
        NetworkKilobitsPerSecondThreshold = NetworkKilobitsPerSecondThreshold,
        NetworkSmoothingWindow = NetworkSmoothingWindow,
        IdleTimeSeconds = IdleTimeSeconds,
    };
}
