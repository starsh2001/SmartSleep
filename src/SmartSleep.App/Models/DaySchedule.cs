using System;

namespace SmartSleep.App.Models;

public class DaySchedule
{
    public bool Enabled { get; set; } = true;
    public bool AllDay { get; set; } = true;
    public TimeSpan StartTime { get; set; } = TimeSpan.Zero;
    public TimeSpan EndTime { get; set; } = TimeSpan.Zero;

    public static DaySchedule CreateDefault() => new();

    public bool IsWithinWindow(DateTime now)
    {
        if (!Enabled)
        {
            return false;
        }

        if (AllDay)
        {
            return true;
        }

        var current = now.TimeOfDay;
        if (StartTime <= EndTime)
        {
            return current >= StartTime && current < EndTime;
        }

        return current >= StartTime || current < EndTime;
    }

    public DaySchedule Clone() => new()
    {
        Enabled = Enabled,
        AllDay = AllDay,
        StartTime = StartTime,
        EndTime = EndTime
    };
}