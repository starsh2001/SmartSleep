using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Utilities;

public class NetworkUsageSampler : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Queue<double> _window = new();
    private double _windowSum;
    private int _windowSize = DefaultValues.NetworkSmoothingWindow;
    private PerformanceCounter? _bytesTotalCounter;
    private bool _disposed;
    private string? _selectedInterface;

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

    public double SampleKilobitsPerSecond()
    {
        if (_disposed)
            return GetAverage();

        lock (_syncRoot)
        {
            try
            {
                // Initialize counter on first use
                if (_bytesTotalCounter == null)
                {
                    if (_selectedInterface == null)
                    {
                        // Get available network interfaces from performance counters
                        var category = new PerformanceCounterCategory("Network Interface");
                        var instanceNames = category.GetInstanceNames();

                        // Find the best interface, excluding system interfaces
                        _selectedInterface = instanceNames
                            .Where(name => !string.IsNullOrEmpty(name) &&
                                          !name.Equals("_Total", StringComparison.OrdinalIgnoreCase) &&
                                          !name.Equals("Loopback Pseudo-Interface 1", StringComparison.OrdinalIgnoreCase) &&
                                          !name.Contains("isatap", StringComparison.OrdinalIgnoreCase) &&
                                          !name.Contains("Teredo", StringComparison.OrdinalIgnoreCase) &&
                                          !name.Contains("VPN", StringComparison.OrdinalIgnoreCase) &&
                                          !name.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(name => name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                                                      name.Contains("Realtek", StringComparison.OrdinalIgnoreCase) ||
                                                      name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                            .FirstOrDefault();

                        // Fallback to _Total if no specific interface found
                        if (_selectedInterface == null)
                        {
                            _selectedInterface = "_Total";
                        }
                    }

                    try
                    {
                        // Use "Bytes Total/sec" counter which matches Task Manager network usage
                        _bytesTotalCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", _selectedInterface);

                        // First call always returns 0, so call once and discard
                        _bytesTotalCounter.NextValue();
                        AddSample(0);
                        return GetAverage();
                    }
                    catch
                    {
                        // Fallback to _Total if specific interface fails
                        try
                        {
                            _selectedInterface = "_Total";
                            _bytesTotalCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", "_Total");
                            _bytesTotalCounter.NextValue();
                            AddSample(0);
                            return GetAverage();
                        }
                        catch
                        {
                            return GetAverage();
                        }
                    }
                }

                var bytesPerSecond = _bytesTotalCounter.NextValue();
                var kbps = (bytesPerSecond * 8) / 1000.0; // Convert bytes/sec to kbps

                AddSample(Math.Max(0, kbps));
                return GetAverage();
            }
            catch
            {
                // Reset counter on error and return average
                _bytesTotalCounter?.Dispose();
                _bytesTotalCounter = null;
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
            _bytesTotalCounter?.Dispose();
            _bytesTotalCounter = null;
            _disposed = true;
        }
    }
}
