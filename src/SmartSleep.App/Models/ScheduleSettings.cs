using System;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Models;

public class ScheduleSettings
{
    public ScheduleMode Mode { get; set; } = DefaultValues.ScheduleMode;

    // Daily mode settings
    public TimeSpan DailyStartTime { get; set; } = DefaultValues.DailyStartTime;
    public TimeSpan DailyEndTime { get; set; } = DefaultValues.DailyEndTime;

    // Weekly mode settings
    public DaySchedule Monday { get; set; } = DefaultValues.CreateDefaultDaySchedule();
    public DaySchedule Tuesday { get; set; } = DefaultValues.CreateDefaultDaySchedule();
    public DaySchedule Wednesday { get; set; } = DefaultValues.CreateDefaultDaySchedule();
    public DaySchedule Thursday { get; set; } = DefaultValues.CreateDefaultDaySchedule();
    public DaySchedule Friday { get; set; } = DefaultValues.CreateDefaultDaySchedule();
    public DaySchedule Saturday { get; set; } = DefaultValues.CreateDefaultDaySchedule();
    public DaySchedule Sunday { get; set; } = DefaultValues.CreateDefaultDaySchedule();

    public static ScheduleSettings CreateDefault() => DefaultValues.CreateDefaultScheduleSettings();

    public bool IsWithinWindow(DateTime now)
    {
        return Mode switch
        {
            ScheduleMode.Always => true,
            ScheduleMode.Daily => IsWithinDailyWindow(now),
            ScheduleMode.Weekly => IsWithinWeeklyWindow(now),
            ScheduleMode.Disabled => false,
            _ => true
        };
    }

    private bool IsWithinDailyWindow(DateTime now)
    {
        var current = now.TimeOfDay;
        if (DailyStartTime <= DailyEndTime)
        {
            return current >= DailyStartTime && current < DailyEndTime;
        }

        return current >= DailyStartTime || current < DailyEndTime;
    }

    private bool IsWithinWeeklyWindow(DateTime now)
    {
        var daySchedule = now.DayOfWeek switch
        {
            DayOfWeek.Monday => Monday,
            DayOfWeek.Tuesday => Tuesday,
            DayOfWeek.Wednesday => Wednesday,
            DayOfWeek.Thursday => Thursday,
            DayOfWeek.Friday => Friday,
            DayOfWeek.Saturday => Saturday,
            DayOfWeek.Sunday => Sunday,
            _ => throw new ArgumentOutOfRangeException()
        };

        return daySchedule.IsWithinWindow(now);
    }

    public ScheduleSettings Clone() => new()
    {
        Mode = Mode,
        DailyStartTime = DailyStartTime,
        DailyEndTime = DailyEndTime,
        Monday = Monday.Clone(),
        Tuesday = Tuesday.Clone(),
        Wednesday = Wednesday.Clone(),
        Thursday = Thursday.Clone(),
        Friday = Friday.Clone(),
        Saturday = Saturday.Clone(),
        Sunday = Sunday.Clone()
    };
}
