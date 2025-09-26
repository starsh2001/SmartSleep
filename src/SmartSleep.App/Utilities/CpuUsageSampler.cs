using System;
using System.Collections.Generic;
using SmartSleep.App.Interop;

namespace SmartSleep.App.Utilities;

public class CpuUsageSampler
{
    private readonly object _syncRoot = new();
    private (ulong Idle, ulong Kernel, ulong User)? _previous;
    private readonly Queue<double> _window = new();
    private double _windowSum;
    private int _windowSize = 3;

    public void SetWindowSize(int windowSize)
    {
        lock (_syncRoot)
        {
            var normalized = Math.Max(1, windowSize);
            if (normalized == _windowSize)
            {
                return;
            }

            _windowSize = normalized;
            TrimWindow();
        }
    }

    public double SampleCpuUsagePercentage()
    {
        if (!NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return GetAverage();
        }

        var idleTicks = ToUInt64(idle);
        var kernelTicks = ToUInt64(kernel);
        var userTicks = ToUInt64(user);

        lock (_syncRoot)
        {
            if (_previous is null)
            {
                _previous = (idleTicks, kernelTicks, userTicks);
                AddSample(0);
                return GetAverage();
            }

            var previous = _previous.Value;
            var idleDelta = Subtract(idleTicks, previous.Idle);
            var kernelDelta = Subtract(kernelTicks, previous.Kernel);
            var userDelta = Subtract(userTicks, previous.User);
            var systemDelta = kernelDelta + userDelta;

            _previous = (idleTicks, kernelTicks, userTicks);

            if (systemDelta <= 0)
            {
                AddSample(0);
                return GetAverage();
            }

            var busy = Math.Max(0, systemDelta - idleDelta);
            var usage = (double)busy * 100.0 / systemDelta;
            AddSample(Math.Clamp(usage, 0, 100));
            return GetAverage();
        }
    }

    private void AddSample(double value)
    {
        _window.Enqueue(value);
        _windowSum += value;
        TrimWindow();
    }

    private void TrimWindow()
    {
        while (_window.Count > _windowSize)
        {
            _windowSum -= _window.Dequeue();
        }
    }

    private double GetAverage()
    {
        if (_window.Count == 0)
        {
            return 0;
        }

        return _windowSum / _window.Count;
    }

    private static ulong ToUInt64(NativeMethods.FILETIME fileTime) => ((ulong)fileTime.dwHighDateTime << 32) | fileTime.dwLowDateTime;

    private static ulong Subtract(ulong current, ulong previous) => current >= previous ? current - previous : current;
}
