using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Utilities;

public class NetworkUsageSampler
{
    private readonly object _syncRoot = new();
    private DateTime _previousTimestamp = DateTime.MinValue;
    private long _previousTotalBytes;
    private readonly Queue<double> _window = new();
    private double _windowSum;
    private int _windowSize = DefaultValues.NetworkSmoothingWindow;

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

    public double SampleKilobytesPerSecond()
    {
        var now = DateTime.UtcNow;
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && !IsIgnored(nic.NetworkInterfaceType))
            .ToList();

        long totalBytes = 0;
        foreach (var nic in interfaces)
        {
            try
            {
                var statistics = nic.GetIPv4Statistics();
                totalBytes += statistics.BytesReceived + statistics.BytesSent;
            }
            catch
            {
                // Ignore interfaces that do not support IPv4 statistics.
            }
        }

        lock (_syncRoot)
        {
            if (_previousTimestamp == DateTime.MinValue)
            {
                _previousTimestamp = now;
                _previousTotalBytes = totalBytes;
                AddSample(0);
                return GetAverage();
            }

            var seconds = (now - _previousTimestamp).TotalSeconds;
            if (seconds <= 0)
            {
                AddSample(0);
                return GetAverage();
            }

            var deltaBytes = totalBytes - _previousTotalBytes;
            if (deltaBytes < 0)
            {
                deltaBytes = 0;
            }

            _previousTimestamp = now;
            _previousTotalBytes = totalBytes;
            var kbps = deltaBytes / seconds / 1024.0;
            AddSample(Math.Max(0, kbps));
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

    private static bool IsIgnored(NetworkInterfaceType type) => type == NetworkInterfaceType.Loopback || type == NetworkInterfaceType.Tunnel;
}
