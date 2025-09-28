using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Utilities;

public class CpuUsageSampler : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Queue<double> _window = new();
    private double _windowSum;
    private int _windowSize = DefaultValues.CpuSmoothingWindow;
    private PerformanceCounter? _cpuCounter;
    private bool _disposed;

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
        if (_disposed)
            return GetAverage();

        lock (_syncRoot)
        {
            try
            {
                // Initialize counter on first use
                if (_cpuCounter == null)
                {
                    try
                    {
                        // Try Processor Information Utility first (Task Manager equivalent)
                        _cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                        // First call always returns 0, so call it once and discard
                        _cpuCounter.NextValue();
                        AddSample(0);
                        return GetAverage();
                    }
                    catch
                    {
                        // Fallback to standard Processor Time if Processor Information is not available
                        try
                        {
                            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                            _cpuCounter.NextValue();
                            AddSample(0);
                            return GetAverage();
                        }
                        catch
                        {
                            return GetAverage();
                        }
                    }
                }

                var cpuValue = _cpuCounter.NextValue();
                AddSample(Math.Clamp(cpuValue, 0, 100));
                return GetAverage();
            }
            catch
            {
                // Reset counter on error and return average
                _cpuCounter?.Dispose();
                _cpuCounter = null;
                return GetAverage();
            }
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

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_syncRoot)
        {
            _cpuCounter?.Dispose();
            _cpuCounter = null;
            _disposed = true;
        }
    }
}
