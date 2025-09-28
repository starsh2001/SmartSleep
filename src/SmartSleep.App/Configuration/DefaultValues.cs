using System;
using SmartSleep.App.Models;

namespace SmartSleep.App.Configuration;

/// <summary>
/// Centralized default values for all application settings and configurations.
/// This class contains all hard-coded values used throughout the application.
/// </summary>
public static class DefaultValues
{
    #region Application Configuration

    /// <summary>
    /// Default power action when idle conditions are met
    /// </summary>
    public static readonly PowerAction PowerAction = PowerAction.Sleep;

    /// <summary>
    /// Default confirmation dialog display setting
    /// </summary>
    public static readonly bool ShowConfirmationDialog = true;

    /// <summary>
    /// Default countdown duration for confirmation dialog (seconds)
    /// </summary>
    public static readonly int ConfirmationCountdownSeconds = 10;

    /// <summary>
    /// Default sleep logging enable setting
    /// </summary>
    public static readonly bool EnableSleepLogging = true;

    /// <summary>
    /// Default auto-start with Windows setting
    /// </summary>
    public static readonly bool StartWithWindows = false;

    /// <summary>
    /// Default monitoring polling interval (seconds)
    /// </summary>
    public static readonly int PollingIntervalSeconds = 1;

    /// <summary>
    /// Default sleep cooldown period (seconds)
    /// </summary>
    public static readonly int SleepCooldownSeconds = 45;

    /// <summary>
    /// Default application language
    /// </summary>
    public static readonly AppLanguage Language = AppLanguage.English;

    #endregion

    #region Idle Detection Settings

    /// <summary>
    /// Default input activity monitoring enable setting
    /// </summary>
    public static readonly bool UseInputActivity = true;

    /// <summary>
    /// Default gamepad input inclusion setting
    /// </summary>
    public static readonly bool IncludeGamepadInput = false;

    /// <summary>
    /// Default CPU activity monitoring enable setting
    /// </summary>
    public static readonly bool UseCpuActivity = true;

    /// <summary>
    /// Default CPU usage threshold percentage
    /// </summary>
    public static readonly double CpuUsagePercentageThreshold = 12.0;

    /// <summary>
    /// Default CPU usage smoothing window size
    /// </summary>
    public static readonly int CpuSmoothingWindow = 10;

    /// <summary>
    /// Default network activity monitoring enable setting
    /// </summary>
    public static readonly bool UseNetworkActivity = true;

    /// <summary>
    /// Default network usage threshold (KB/s)
    /// </summary>
    public static readonly double NetworkKilobytesPerSecondThreshold = 200.0;

    /// <summary>
    /// Default network usage smoothing window size
    /// </summary>
    public static readonly int NetworkSmoothingWindow = 10;

    /// <summary>
    /// Default idle time requirement for all conditions (seconds)
    /// </summary>
    public static readonly int IdleTimeSeconds = 1200; // 20 minutes

    #endregion

    #region Schedule Settings

    /// <summary>
    /// Default schedule mode
    /// </summary>
    public static readonly ScheduleMode ScheduleMode = ScheduleMode.Always;

    /// <summary>
    /// Default daily schedule start time (22:00)
    /// </summary>
    public static readonly TimeSpan DailyStartTime = new(22, 0, 0);

    /// <summary>
    /// Default daily schedule end time (06:00)
    /// </summary>
    public static readonly TimeSpan DailyEndTime = new(6, 0, 0);

    /// <summary>
    /// Default day schedule enabled setting
    /// </summary>
    public static readonly bool DayScheduleEnabled = true;

    /// <summary>
    /// Default day schedule all-day setting
    /// </summary>
    public static readonly bool DayScheduleAllDay = true;

    /// <summary>
    /// Default day schedule start time
    /// </summary>
    public static readonly TimeSpan DayScheduleStartTime = TimeSpan.Zero;

    /// <summary>
    /// Default day schedule end time
    /// </summary>
    public static readonly TimeSpan DayScheduleEndTime = TimeSpan.Zero;

    #endregion

    #region UI and Timing Constants

    /// <summary>
    /// Default tooltip hide delay (milliseconds)
    /// </summary>
    public static readonly int TooltipHideDelayMs = 2000;

    /// <summary>
    /// Default mouse leave check interval (milliseconds)
    /// </summary>
    public static readonly int MouseLeaveCheckIntervalMs = 200;

    /// <summary>
    /// Default input monitoring interval (milliseconds)
    /// </summary>
    public static readonly int InputMonitoringIntervalMs = 50;

    /// <summary>
    /// Default mouse monitoring interval (milliseconds)
    /// </summary>
    public static readonly int MouseMonitoringIntervalMs = 50;

    /// <summary>
    /// Default confirmation timer interval (milliseconds)
    /// </summary>
    public static readonly int ConfirmationTimerIntervalMs = 1000;

    /// <summary>
    /// Default mouse monitoring stop timeout (seconds)
    /// </summary>
    public static readonly int MouseMonitoringStopTimeoutSeconds = 1;

    /// <summary>
    /// Default minimum sleep cooldown (seconds)
    /// </summary>
    public static readonly int MinimumSleepCooldownSeconds = 10;

    /// <summary>
    /// Default maximum sleep log entries
    /// </summary>
    public static readonly int MaxSleepLogEntries = 100;

    #endregion

    #region Native API Constants

    /// <summary>
    /// Default notify icon string size limit
    /// </summary>
    public static readonly int NotifyIconStringSize = 128;

    /// <summary>
    /// Default notify icon info string size limit
    /// </summary>
    public static readonly int NotifyIconInfoStringSize = 256;

    /// <summary>
    /// Default notify icon title string size limit
    /// </summary>
    public static readonly int NotifyIconTitleStringSize = 64;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a new AppConfig instance with all default values
    /// </summary>
    public static AppConfig CreateDefaultAppConfig() => new()
    {
        Idle = CreateDefaultIdleSettings(),
        Schedule = CreateDefaultScheduleSettings(),
        PowerAction = PowerAction,
        ShowConfirmationDialog = ShowConfirmationDialog,
        ConfirmationCountdownSeconds = ConfirmationCountdownSeconds,
        EnableSleepLogging = EnableSleepLogging,
        StartWithWindows = StartWithWindows,
        PollingIntervalSeconds = PollingIntervalSeconds,
        SleepCooldownSeconds = SleepCooldownSeconds,
        Language = Language
    };

    /// <summary>
    /// Creates a new IdleSettings instance with all default values
    /// </summary>
    public static IdleSettings CreateDefaultIdleSettings() => new()
    {
        UseInputActivity = UseInputActivity,
        IncludeGamepadInput = IncludeGamepadInput,
        UseCpuActivity = UseCpuActivity,
        CpuUsagePercentageThreshold = CpuUsagePercentageThreshold,
        CpuSmoothingWindow = CpuSmoothingWindow,
        UseNetworkActivity = UseNetworkActivity,
        NetworkKilobytesPerSecondThreshold = NetworkKilobytesPerSecondThreshold,
        NetworkSmoothingWindow = NetworkSmoothingWindow,
        IdleTimeSeconds = IdleTimeSeconds
    };

    /// <summary>
    /// Creates a new ScheduleSettings instance with all default values
    /// </summary>
    public static ScheduleSettings CreateDefaultScheduleSettings() => new()
    {
        Mode = ScheduleMode,
        DailyStartTime = DailyStartTime,
        DailyEndTime = DailyEndTime,
        Monday = CreateDefaultDaySchedule(),
        Tuesday = CreateDefaultDaySchedule(),
        Wednesday = CreateDefaultDaySchedule(),
        Thursday = CreateDefaultDaySchedule(),
        Friday = CreateDefaultDaySchedule(),
        Saturday = CreateDefaultDaySchedule(),
        Sunday = CreateDefaultDaySchedule()
    };

    /// <summary>
    /// Creates a new DaySchedule instance with all default values
    /// </summary>
    public static DaySchedule CreateDefaultDaySchedule() => new()
    {
        Enabled = DayScheduleEnabled,
        AllDay = DayScheduleAllDay,
        StartTime = DayScheduleStartTime,
        EndTime = DayScheduleEndTime
    };

    #endregion
}