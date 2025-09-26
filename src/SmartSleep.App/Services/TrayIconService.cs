using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using SmartSleep.App.Models;
using SmartSleep.App.Views;
using Forms = System.Windows.Forms;

namespace SmartSleep.App.Services;

public class TrayIconService : IDisposable
{
    private readonly MonitoringService _monitoringService;
    private readonly Func<SettingsWindow> _settingsWindowFactory;
    private readonly Dispatcher _dispatcher;
    private Forms.NotifyIcon? _notifyIcon;
    private SettingsWindow? _settingsWindow;
    private System.Drawing.Icon? _iconResource;

    public TrayIconService(MonitoringService monitoringService,
                           Func<SettingsWindow> settingsWindowFactory,
                           Dispatcher dispatcher)
    {
        _monitoringService = monitoringService;
        _settingsWindowFactory = settingsWindowFactory;
        _dispatcher = dispatcher;
    }

    public void Initialize()
    {
        _iconResource = LoadIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _iconResource ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "SmartSleep"
        };

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("설정 열기", null, (_, _) => ShowSettings());
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add("종료", null, (_, _) => ExitApplication());
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _monitoringService.SnapshotAvailable += MonitoringServiceOnSnapshotAvailable;
        _monitoringService.SleepTriggered += MonitoringServiceOnSleepTriggered;
    }

    private System.Drawing.Icon? LoadIcon()
    {
        try
        {
            var resourceUri = new Uri("pack://application:,,,/resources/smartsleep.ico", UriKind.Absolute);
            var resourceInfo = System.Windows.Application.GetResourceStream(resourceUri);
            if (resourceInfo?.Stream == null)
            {
                return null;
            }

            using var memory = new MemoryStream();
            resourceInfo.Stream.CopyTo(memory);
            memory.Position = 0;
            return new System.Drawing.Icon(memory);
        }
        catch
        {
            return null;
        }
    }

    private void MonitoringServiceOnSnapshotAvailable(object? sender, MonitoringSnapshot snapshot)
    {
        if (_notifyIcon == null)
        {
            return;
        }

        var tooltip = BuildTooltip(snapshot);
        _dispatcher.BeginInvoke(() =>
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = tooltip;
            }
        });
    }

    private void MonitoringServiceOnSleepTriggered(object? sender, string message)
    {
        if (_notifyIcon == null)
        {
            return;
        }

        _dispatcher.BeginInvoke(() =>
        {
            if (_notifyIcon == null)
            {
                return;
            }

            _notifyIcon.BalloonTipTitle = "SmartSleep";
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(3000);
        });
    }

    private void ShowSettings()
    {
        _dispatcher.BeginInvoke(() =>
        {
            if (_settingsWindow is { IsVisible: true })
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = _settingsWindowFactory();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
        });
    }

    private void ExitApplication()
    {
        _dispatcher.BeginInvoke(new Action(async () =>
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            try
            {
                await _monitoringService.StopAsync().ConfigureAwait(true);
            }
            catch
            {
                // Ignore shutdown exceptions.
            }

            System.Windows.Application.Current.Shutdown();
        }));
    }

    private static string BuildTooltip(MonitoringSnapshot snapshot)
    {
        var lines = new List<string> { "SmartSleep" };

        lines.Add(snapshot.InputMonitoringEnabled
            ? $"입력 {snapshot.InputIdle.TotalSeconds:F0}/{snapshot.InputIdleRequirement.TotalSeconds:F0}s"
            : $"입력 OFF {snapshot.InputIdle.TotalSeconds:F0}s");

        lines.Add(snapshot.CpuMonitoringEnabled
            ? $"CPU {snapshot.CpuUsagePercent:F1}/{snapshot.CpuThresholdPercent:F1}% {snapshot.CpuIdleDuration.TotalSeconds:F0}/{snapshot.CpuIdleRequirement.TotalSeconds:F0}s"
            : $"CPU OFF {snapshot.CpuUsagePercent:F1}%");

        lines.Add(snapshot.NetworkMonitoringEnabled
            ? $"네트워크 {snapshot.NetworkKilobytesPerSecond:F0}/{snapshot.NetworkThresholdKilobytesPerSecond:F0}KB {snapshot.NetworkIdleDuration.TotalSeconds:F0}/{snapshot.NetworkIdleRequirement.TotalSeconds:F0}s"
            : $"네트워크 OFF {snapshot.NetworkKilobytesPerSecond:F0}KB");

        if (snapshot.EnabledConditionCount > 0)
        {
            var modeLabel = snapshot.CombinationMode == IdleCombinationMode.All ? "AND" : "OR";
            lines.Add($"{modeLabel} {snapshot.SatisfiedConditionCount}/{snapshot.EnabledConditionCount}");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.StatusMessage))
        {
            lines.Add(snapshot.StatusMessage);
        }

        var text = string.Join('\n', lines);
        return text.Length > 63 ? text[..63] : text;
    }

    public void Dispose()
    {
        _monitoringService.SnapshotAvailable -= MonitoringServiceOnSnapshotAvailable;
        _monitoringService.SleepTriggered -= MonitoringServiceOnSleepTriggered;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _iconResource?.Dispose();
        _iconResource = null;
    }
}
