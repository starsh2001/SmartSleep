using System;

namespace SmartSleep.App.Models;

public class MonitoringSnapshot
{
    public DateTime Timestamp { get; init; }

    public bool InputMonitoringEnabled { get; init; }
    public TimeSpan InputIdle { get; init; }
    public TimeSpan InputIdleRequirement { get; init; }
    public bool InputConditionMet { get; init; }

    public bool CpuMonitoringEnabled { get; init; }
    public double CpuUsagePercent { get; init; }
    public double CpuThresholdPercent { get; init; }
    public TimeSpan CpuIdleDuration { get; init; }
    public TimeSpan CpuIdleRequirement { get; init; }
    public bool CpuConditionMet { get; init; }

    public bool NetworkMonitoringEnabled { get; init; }
    public double NetworkKilobytesPerSecond { get; init; }
    public double NetworkThresholdKilobytesPerSecond { get; init; }
    public TimeSpan NetworkIdleDuration { get; init; }
    public TimeSpan NetworkIdleRequirement { get; init; }
    public bool NetworkConditionMet { get; init; }

    public IdleCombinationMode CombinationMode { get; init; }
    public int EnabledConditionCount { get; init; }
    public int SatisfiedConditionCount { get; init; }
    public bool ScheduleActive { get; init; }
    public bool ConditionsMet { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
}
