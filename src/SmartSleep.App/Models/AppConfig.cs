using System;

namespace SmartSleep.App.Models;

public class AppConfig
{
    public IdleSettings Idle { get; set; } = IdleSettings.CreateDefault();
    public ScheduleSettings Schedule { get; set; } = ScheduleSettings.CreateDefault();
    public bool StartWithWindows { get; set; }
    public int PollingIntervalSeconds { get; set; } = 1;
    public int SleepCooldownSeconds { get; set; } = 45;

    public static AppConfig CreateDefault() => new()
    {
        Idle = IdleSettings.CreateDefault(),
        Schedule = ScheduleSettings.CreateDefault(),
        StartWithWindows = false,
        PollingIntervalSeconds = 1,
        SleepCooldownSeconds = 45
    };

    public AppConfig Clone() => new()
    {
        Idle = Idle.Clone(),
        Schedule = Schedule.Clone(),
        StartWithWindows = StartWithWindows,
        PollingIntervalSeconds = PollingIntervalSeconds,
        SleepCooldownSeconds = SleepCooldownSeconds
    };
}
