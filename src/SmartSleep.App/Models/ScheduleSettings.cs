using System;

namespace SmartSleep.App.Models;

public class ScheduleSettings
{
    public bool Enabled { get; set; }
    public TimeSpan StartTime { get; set; } = TimeSpan.Zero;
    public TimeSpan EndTime { get; set; } = TimeSpan.Zero;

    public static ScheduleSettings CreateDefault() => new();

    public bool IsWithinWindow(DateTime now)
    {
        if (!Enabled)
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

    public ScheduleSettings Clone() => new()
    {
        Enabled = Enabled,
        StartTime = StartTime,
        EndTime = EndTime
    };
}
