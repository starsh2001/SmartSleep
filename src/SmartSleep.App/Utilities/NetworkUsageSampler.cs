using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Utilities;

public class NetworkUsageSampler
{
    private readonly object _syncRoot = new();
    private readonly Queue<double> _window = new();
    private double _windowSum;
    private int _windowSize = DefaultValues.NetworkSmoothingWindow;
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
        lock (_syncRoot)
        {
            try
            {
                if (_selectedInterface == null)
                {
                    // Get available network interfaces from WMI
                    using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_PerfFormattedData_Tcpip_NetworkInterface");
                    using var results = searcher.Get();

                    var interfaceNames = results
                        .Cast<ManagementObject>()
                        .Select(mo => mo["Name"]?.ToString())
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToArray();

                    _selectedInterface = interfaceNames
                        .Where(name => name != null &&
                                      !name.Equals("Loopback Pseudo-Interface 1", StringComparison.OrdinalIgnoreCase) &&
                                      !name.Contains("isatap") &&
                                      !name.Contains("Teredo") &&
                                      !name.Contains("UsbNcm") &&
                                      !name.Contains("VPN") &&
                                      !name.Contains("Virtual"))
                        .OrderByDescending(name => name?.Contains("Ethernet") == true || name?.Contains("Realtek") == true || name?.Contains("Intel") == true)
                        .FirstOrDefault();

                    if (_selectedInterface == null)
                    {
                        return GetAverage();
                    }

                    return GetAverage();
                }

                // Get network usage directly from WMI - same as Task Manager
                using var usageSearcher = new ManagementObjectSearcher($"SELECT BytesTotalPerSec FROM Win32_PerfFormattedData_Tcpip_NetworkInterface WHERE Name='{_selectedInterface.Replace("'", "''")}'");
                using var usageResults = usageSearcher.Get();

                var networkUsage = usageResults
                    .Cast<ManagementObject>()
                    .FirstOrDefault()?
                    .Properties["BytesTotalPerSec"]?
                    .Value;

                if (networkUsage == null)
                {
                    return GetAverage();
                }

                var bytesPerSecond = Convert.ToDouble(networkUsage);
                var kbps = (bytesPerSecond * 8) / 1000.0; // Convert bytes/sec to kbps
                AddSample(Math.Max(0, kbps));
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
