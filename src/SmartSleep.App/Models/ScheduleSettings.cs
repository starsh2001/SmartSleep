using System;

namespace SmartSleep.App.Models;

public class ScheduleSettings
{
    public ScheduleMode Mode { get; set; } = ScheduleMode.Always;

    // Daily mode settings
    public TimeSpan DailyStartTime { get; set; } = TimeSpan.Zero;
    public TimeSpan DailyEndTime { get; set; } = TimeSpan.Zero;

    // Weekly mode settings
    public DaySchedule Monday { get; set; } = DaySchedule.CreateDefault();
    public DaySchedule Tuesday { get; set; } = DaySchedule.CreateDefault();
    public DaySchedule Wednesday { get; set; } = DaySchedule.CreateDefault();
    public DaySchedule Thursday { get; set; } = DaySchedule.CreateDefault();
    public DaySchedule Friday { get; set; } = DaySchedule.CreateDefault();
    public DaySchedule Saturday { get; set; } = DaySchedule.CreateDefault();
    public DaySchedule Sunday { get; set; } = DaySchedule.CreateDefault();

    public static ScheduleSettings CreateDefault() => new();

    public bool IsWithinWindow(DateTime now)
    {
        return Mode switch
        {
            ScheduleMode.Always => true,
            ScheduleMode.Daily => IsWithinDailyWindow(now),
            ScheduleMode.Weekly => IsWithinWeeklyWindow(now),
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
