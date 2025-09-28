using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Utilities;

public class CpuUsageSampler
{
    private readonly object _syncRoot = new();
    private readonly Queue<double> _window = new();
    private double _windowSum;
    private int _windowSize = DefaultValues.CpuSmoothingWindow;

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
        lock (_syncRoot)
        {
            try
            {
                // Use Task Manager equivalent: Processor Information Utility
                double cpuValue = 0;

                // First try: Processor Information % Processor Utility (Task Manager equivalent)
                try
                {
                    using var searcher1 = new ManagementObjectSearcher("SELECT PercentProcessorUtility FROM Win32_PerfFormattedData_Counters_ProcessorInformation WHERE Name='_Total'");
                    using var results1 = searcher1.Get();

                    var processorUtility = results1
                        .Cast<ManagementObject>()
                        .FirstOrDefault()?
                        .Properties["PercentProcessorUtility"]?
                        .Value;

                    if (processorUtility != null)
                    {
                        cpuValue = Convert.ToDouble(processorUtility);
                    }
                    else
                    {
                        throw new Exception("Processor Utility not available");
                    }
                }
                catch
                {
                    // Fallback to standard PercentProcessorTime if Processor Information is not available
                    try
                    {
                        using var searcher2 = new ManagementObjectSearcher("SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
                        using var results2 = searcher2.Get();

                        var cpuUsage = results2
                            .Cast<ManagementObject>()
                            .First()
                            .Properties["PercentProcessorTime"]
                            .Value;

                        cpuValue = Convert.ToDouble(cpuUsage);
                    }
                    catch
                    {
                        return GetAverage();
                    }
                }

                AddSample(Math.Clamp(cpuValue, 0, 100));
                return GetAverage();
            }
            catch
            {
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

}
