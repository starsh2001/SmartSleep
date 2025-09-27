using System;

namespace SmartSleep.App.Models;

public class AppConfig
{
    public IdleSettings Idle { get; set; } = IdleSettings.CreateDefault();
    public ScheduleSettings Schedule { get; set; } = ScheduleSettings.CreateDefault();
    public PowerAction PowerAction { get; set; } = PowerAction.Sleep;
    public bool ShowConfirmationDialog { get; set; } = false;
    public int ConfirmationCountdownSeconds { get; set; } = 10;
    public bool StartWithWindows { get; set; }
    public int PollingIntervalSeconds { get; set; } = 1;
    public int SleepCooldownSeconds { get; set; } = 45;

    public static AppConfig CreateDefault() => new()
    {
        Idle = IdleSettings.CreateDefault(),
        Schedule = ScheduleSettings.CreateDefault(),
        PowerAction = PowerAction.Sleep,
        ShowConfirmationDialog = false,
        ConfirmationCountdownSeconds = 10,
        StartWithWindows = false,
        PollingIntervalSeconds = 1,
        SleepCooldownSeconds = 45
    };

    public AppConfig Clone() => new()
    {
        Idle = Idle.Clone(),
        Schedule = Schedule.Clone(),
        PowerAction = PowerAction,
        ShowConfirmationDialog = ShowConfirmationDialog,
        ConfirmationCountdownSeconds = ConfirmationCountdownSeconds,
        StartWithWindows = StartWithWindows,
        PollingIntervalSeconds = PollingIntervalSeconds,
        SleepCooldownSeconds = SleepCooldownSeconds
    };
}
