using System;
using SmartSleep.App.Models;
using SmartSleep.App.Utilities;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace SmartSleep.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _useInputActivity;
    private int _inputIdleSeconds = 1200;
    private bool _useCpuActivity;
    private double _cpuUsageThreshold = 10.0;
    private int _cpuIdleDurationSeconds = 600;
    private int _cpuSmoothingWindow = 5;
    private bool _useNetworkActivity;
    private double _networkThreshold = 128.0;
    private int _networkIdleDurationSeconds = 600;
    private int _networkSmoothingWindow = 5;
    private int _pollingIntervalSeconds;
    private bool _scheduleEnabled;
    private string _scheduleStartText = "00:00";
    private string _scheduleEndText = "00:00";
    private bool _startWithWindows;
    private int _sleepCooldownSeconds = 45;
    private string _statusMessage = string.Empty;

    private string _liveInputStatus = "입력 유휴: 수집 중";
    private string _liveCpuStatus = string.Empty;
    private string _liveNetworkStatus = string.Empty;
    private string _liveCombinationStatus = string.Empty;
    private string _liveStatusMessage = string.Empty;
    private Brush _liveStatusBrush = Brushes.SlateGray;
    private MonitoringSnapshot? _lastSnapshot;

    public bool UseInputActivity
    {
        get => _useInputActivity;
        set
        {
            if (SetProperty(ref _useInputActivity, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public int InputIdleSeconds
    {
        get => _inputIdleSeconds;
        set
        {
            if (SetProperty(ref _inputIdleSeconds, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public bool UseCpuActivity
    {
        get => _useCpuActivity;
        set
        {
            if (SetProperty(ref _useCpuActivity, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public double CpuUsageThreshold
    {
        get => _cpuUsageThreshold;
        set
        {
            if (SetProperty(ref _cpuUsageThreshold, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public int CpuIdleDurationSeconds
    {
        get => _cpuIdleDurationSeconds;
        set
        {
            if (SetProperty(ref _cpuIdleDurationSeconds, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public int CpuSmoothingWindow
    {
        get => _cpuSmoothingWindow;
        set
        {
            if (SetProperty(ref _cpuSmoothingWindow, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public bool UseNetworkActivity
    {
        get => _useNetworkActivity;
        set
        {
            if (SetProperty(ref _useNetworkActivity, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public double NetworkThreshold
    {
        get => _networkThreshold;
        set
        {
            if (SetProperty(ref _networkThreshold, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public int NetworkIdleDurationSeconds
    {
        get => _networkIdleDurationSeconds;
        set
        {
            if (SetProperty(ref _networkIdleDurationSeconds, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public int NetworkSmoothingWindow
    {
        get => _networkSmoothingWindow;
        set
        {
            if (SetProperty(ref _networkSmoothingWindow, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public int PollingIntervalSeconds
    {
        get => _pollingIntervalSeconds;
        set => SetProperty(ref _pollingIntervalSeconds, value);
    }

    public bool ScheduleEnabled
    {
        get => _scheduleEnabled;
        set => SetProperty(ref _scheduleEnabled, value);
    }

    public string ScheduleStartText
    {
        get => _scheduleStartText;
        set => SetProperty(ref _scheduleStartText, value);
    }

    public string ScheduleEndText
    {
        get => _scheduleEndText;
        set => SetProperty(ref _scheduleEndText, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }


    public int SleepCooldownSeconds
    {
        get => _sleepCooldownSeconds;
        set => SetProperty(ref _sleepCooldownSeconds, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string LiveInputStatus
    {
        get => _liveInputStatus;
        private set => SetProperty(ref _liveInputStatus, value);
    }

    public string LiveCpuStatus
    {
        get => _liveCpuStatus;
        private set => SetProperty(ref _liveCpuStatus, value);
    }

    public string LiveNetworkStatus
    {
        get => _liveNetworkStatus;
        private set => SetProperty(ref _liveNetworkStatus, value);
    }

    public string LiveCombinationStatus
    {
        get => _liveCombinationStatus;
        private set => SetProperty(ref _liveCombinationStatus, value);
    }

    public string LiveStatusMessage
    {
        get => _liveStatusMessage;
        private set => SetProperty(ref _liveStatusMessage, value);
    }
    public Brush LiveStatusBrush
    {
        get => _liveStatusBrush;
        private set => SetProperty(ref _liveStatusBrush, value);
    }


    public static SettingsViewModel FromConfig(AppConfig config)
    {
        var cooldown = config.SleepCooldownSeconds >= 10 ? config.SleepCooldownSeconds : 45;

        return new SettingsViewModel
        {
            UseInputActivity = config.Idle.UseInputActivity,
            InputIdleSeconds = config.Idle.InputIdleThresholdSeconds,
            UseCpuActivity = config.Idle.UseCpuActivity,
            CpuUsageThreshold = config.Idle.CpuUsagePercentageThreshold,
            CpuIdleDurationSeconds = config.Idle.CpuIdleDurationSeconds,
            CpuSmoothingWindow = config.Idle.CpuSmoothingWindow,
            UseNetworkActivity = config.Idle.UseNetworkActivity,
            NetworkThreshold = config.Idle.NetworkKilobytesPerSecondThreshold,
            NetworkIdleDurationSeconds = config.Idle.NetworkIdleDurationSeconds,
            NetworkSmoothingWindow = config.Idle.NetworkSmoothingWindow,
            PollingIntervalSeconds = config.PollingIntervalSeconds,
            ScheduleEnabled = config.Schedule.Enabled,
            ScheduleStartText = config.Schedule.StartTime.ToString("hh\\:mm"),
            ScheduleEndText = config.Schedule.EndTime.ToString("hh\\:mm"),
            StartWithWindows = config.StartWithWindows,
            SleepCooldownSeconds = cooldown
        };
    }

    public bool TryValidate(out string validationError)
    {
        if (InputIdleSeconds < 0)
        {
            validationError = "입력 유휴 시간(초)은 0 이상이어야 합니다.";
            return false;
        }

        if (UseCpuActivity)
        {
            if (CpuUsageThreshold < 0 || CpuUsageThreshold > 100)
            {
                validationError = "CPU 임계값은 0에서 100 사이여야 합니다.";
                return false;
            }

            if (CpuIdleDurationSeconds < 0)
            {
                validationError = "CPU 유휴 시간(초)은 0 이상이어야 합니다.";
                return false;
            }
        }

        if (CpuSmoothingWindow <= 0)
        {
            validationError = "CPU 이동 평균 샘플 수는 1 이상이어야 합니다.";
            return false;
        }

        if (UseNetworkActivity)
        {
            if (NetworkThreshold < 0)
            {
                validationError = "네트워크 임계값은 0 이상이어야 합니다.";
                return false;
            }

            if (NetworkIdleDurationSeconds < 0)
            {
                validationError = "네트워크 유휴 시간(초)은 0 이상이어야 합니다.";
                return false;
            }
        }

        if (NetworkSmoothingWindow <= 0)
        {
            validationError = "네트워크 이동 평균 샘플 수는 1 이상이어야 합니다.";
            return false;
        }

        if (SleepCooldownSeconds < 10)
        {
            validationError = "절전 재시도 대기 시간은 10초 이상이어야 합니다.";
            return false;
        }

        if (!TimeSpan.TryParse(ScheduleStartText, out _) || !TimeSpan.TryParse(ScheduleEndText, out _))
        {
            validationError = "감시 시간대는 HH:mm 형식으로 입력해야 합니다.";
            return false;
        }

        if (PollingIntervalSeconds <= 0)
        {
            validationError = "모니터링 주기는 0보다 커야 합니다.";
            return false;
        }

        validationError = string.Empty;
        return true;
    }

    public AppConfig ToConfig(AppConfig existing)
    {
        var config = existing.Clone();
        config.Idle.UseInputActivity = UseInputActivity;
        config.Idle.InputIdleThresholdSeconds = InputIdleSeconds;
        config.Idle.UseCpuActivity = UseCpuActivity;
        config.Idle.CpuUsagePercentageThreshold = CpuUsageThreshold;
        config.Idle.CpuIdleDurationSeconds = CpuIdleDurationSeconds;
        config.Idle.CpuSmoothingWindow = CpuSmoothingWindow;
        config.Idle.UseNetworkActivity = UseNetworkActivity;
        config.Idle.NetworkKilobytesPerSecondThreshold = NetworkThreshold;
        config.Idle.NetworkIdleDurationSeconds = NetworkIdleDurationSeconds;
        config.Idle.NetworkSmoothingWindow = NetworkSmoothingWindow;
        config.PollingIntervalSeconds = PollingIntervalSeconds;
        config.StartWithWindows = StartWithWindows;
        config.SleepCooldownSeconds = Math.Max(10, SleepCooldownSeconds);

        if (TimeSpan.TryParse(ScheduleStartText, out var start))
        {
            config.Schedule.StartTime = start;
        }

        if (TimeSpan.TryParse(ScheduleEndText, out var end))
        {
            config.Schedule.EndTime = end;
        }

        config.Schedule.Enabled = ScheduleEnabled;
        return config;
    }

    public void UpdateLiveSnapshot(MonitoringSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        RefreshLiveStatus();
    }

    private void RefreshLiveStatus()
    {
        if (_lastSnapshot == null)
        {
            LiveInputStatus = "입력 유휴: 수집 중";
            LiveCpuStatus = string.Empty;
            LiveNetworkStatus = string.Empty;
            LiveCombinationStatus = string.Empty;
            LiveStatusMessage = string.Empty;
            LiveStatusBrush = Brushes.SlateGray;
            return;
        }

        var snapshot = _lastSnapshot;

        if (snapshot.InputMonitoringEnabled)
        {
            LiveInputStatus = $"입력 유휴 {snapshot.InputIdle.TotalSeconds:F0}s / {snapshot.InputIdleRequirement.TotalSeconds:F0}s";
        }
        else
        {
            LiveInputStatus = $"입력 유휴 {snapshot.InputIdle.TotalSeconds:F0}s (미감시)";
        }

        if (snapshot.CpuMonitoringEnabled)
        {
            LiveCpuStatus = $"CPU {snapshot.CpuUsagePercent:F1}% / {snapshot.CpuThresholdPercent:F1}% | 유휴 {snapshot.CpuIdleDuration.TotalSeconds:F0}/{snapshot.CpuIdleRequirement.TotalSeconds:F0}s";
        }
        else
        {
            LiveCpuStatus = $"CPU {snapshot.CpuUsagePercent:F1}% (미감시)";
        }

        if (snapshot.NetworkMonitoringEnabled)
        {
            LiveNetworkStatus = $"네트워크 {snapshot.NetworkKilobytesPerSecond:F0}KB/s / {snapshot.NetworkThresholdKilobytesPerSecond:F0}KB/s | 유휴 {snapshot.NetworkIdleDuration.TotalSeconds:F0}/{snapshot.NetworkIdleRequirement.TotalSeconds:F0}s";
        }
        else
        {
            LiveNetworkStatus = $"네트워크 {snapshot.NetworkKilobytesPerSecond:F0}KB/s (미감시)";
        }

        if (snapshot.EnabledConditionCount > 0)
        {
            LiveCombinationStatus = $"조건 {snapshot.SatisfiedConditionCount}/{snapshot.EnabledConditionCount}";
        }
        else
        {
            LiveCombinationStatus = "조건 비활성화";
        }

        var statusText = snapshot.StatusMessage;
        if (!string.IsNullOrWhiteSpace(statusText) && snapshot.InputMonitoringEnabled && statusText.StartsWith("입력 유휴", StringComparison.Ordinal))
        {
            statusText = string.Empty;
        }

        var (displayText, brush) = StatusDisplayHelper.FormatStatus(statusText);
        LiveStatusMessage = displayText;
        LiveStatusBrush = brush;
    }
}
