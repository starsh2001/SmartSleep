using System;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Models;

public class DaySchedule
{
    public bool Enabled { get; set; } = DefaultValues.DayScheduleEnabled;
    public bool AllDay { get; set; } = DefaultValues.DayScheduleAllDay;
    public TimeSpan StartTime { get; set; } = DefaultValues.DayScheduleStartTime;
    public TimeSpan EndTime { get; set; } = DefaultValues.DayScheduleEndTime;

    public static DaySchedule CreateDefault() => DefaultValues.CreateDefaultDaySchedule();

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