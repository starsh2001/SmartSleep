using System;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Models;

public class AppConfig
{
    public IdleSettings Idle { get; set; } = DefaultValues.CreateDefaultIdleSettings();
    public ScheduleSettings Schedule { get; set; } = DefaultValues.CreateDefaultScheduleSettings();
    public PowerAction PowerAction { get; set; } = DefaultValues.PowerAction;
    public bool ShowConfirmationDialog { get; set; } = DefaultValues.ShowConfirmationDialog;
    public int ConfirmationCountdownSeconds { get; set; } = DefaultValues.ConfirmationCountdownSeconds;
    public bool EnableSleepLogging { get; set; } = DefaultValues.EnableSleepLogging;
    public bool StartWithWindows { get; set; } = DefaultValues.StartWithWindows;
    public int PollingIntervalSeconds { get; set; } = DefaultValues.PollingIntervalSeconds;
    public int SleepCooldownSeconds { get; set; } = DefaultValues.SleepCooldownSeconds;
    public AppLanguage Language { get; set; } = DefaultValues.Language;

    public static AppConfig CreateDefault() => DefaultValues.CreateDefaultAppConfig();

    public AppConfig Clone() => new()
    {
        Idle = Idle.Clone(),
        Schedule = Schedule.Clone(),
        PowerAction = PowerAction,
        ShowConfirmationDialog = ShowConfirmationDialog,
        ConfirmationCountdownSeconds = ConfirmationCountdownSeconds,
        EnableSleepLogging = EnableSleepLogging,
        StartWithWindows = StartWithWindows,
        PollingIntervalSeconds = PollingIntervalSeconds,
        SleepCooldownSeconds = SleepCooldownSeconds,
        Language = Language
    };
}
