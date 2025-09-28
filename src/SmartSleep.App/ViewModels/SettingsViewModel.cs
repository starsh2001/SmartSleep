using System;
using System.Collections.Generic;
using SmartSleep.App.Models;
using SmartSleep.App.Utilities;
using System.Windows.Media;
using System.Windows;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace SmartSleep.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{

    private bool _useInputActivity;
    private bool _includeGamepadInput = true;
    private bool _useCpuActivity;
    private double _cpuUsageThreshold = 10.0;
    private int _cpuSmoothingWindow = 5;
    private bool _useNetworkActivity;
    private double _networkThreshold = 128.0;
    private int _networkSmoothingWindow = 5;

    // Unified idle time for all conditions
    private int _idleTimeSeconds = 1200;
    private int _pollingIntervalSeconds;

    private MonitoringSnapshot? _previousSnapshot;
    // Schedule mode
    private ScheduleMode _scheduleMode = ScheduleMode.Always;
    private string _dailyStartText = "22:00";
    private string _dailyEndText = "06:00";

    // Weekly schedule properties
    private bool _mondayEnabled = true;
    private bool _mondayAllDay = true;
    private string _mondayStartText = "00:00";
    private string _mondayEndText = "00:00";

    private bool _tuesdayEnabled = true;
    private bool _tuesdayAllDay = true;
    private string _tuesdayStartText = "00:00";
    private string _tuesdayEndText = "00:00";

    private bool _wednesdayEnabled = true;
    private bool _wednesdayAllDay = true;
    private string _wednesdayStartText = "00:00";
    private string _wednesdayEndText = "00:00";

    private bool _thursdayEnabled = true;
    private bool _thursdayAllDay = true;
    private string _thursdayStartText = "00:00";
    private string _thursdayEndText = "00:00";

    private bool _fridayEnabled = true;
    private bool _fridayAllDay = true;
    private string _fridayStartText = "00:00";
    private string _fridayEndText = "00:00";

    private bool _saturdayEnabled = true;
    private bool _saturdayAllDay = true;
    private string _saturdayStartText = "00:00";
    private string _saturdayEndText = "00:00";

    private bool _sundayEnabled = true;
    private bool _sundayAllDay = true;
    private string _sundayStartText = "00:00";
    private string _sundayEndText = "00:00";
    private bool _startWithWindows;
    private PowerAction _powerAction = PowerAction.Sleep;
    private bool _showConfirmationDialog = false;
    private int _confirmationCountdownSeconds = 10;
    private bool _enableSleepLogging = true;
    private int _sleepCooldownSeconds = 45;
    private string _statusMessage = string.Empty;

    private AppLanguage _language = AppLanguage.English;
    private string _liveInputStatus = "입력 유휴: 수집 중";
    private string _liveCpuStatus = string.Empty;
    private string _liveNetworkStatus = string.Empty;
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

    public bool IncludeGamepadInput
    {
        get => _includeGamepadInput;
        set
        {
            if (SetProperty(ref _includeGamepadInput, value))
            {
                RefreshLiveStatus();
            }
        }
    }

    public int IdleTimeSeconds
    {
        get => _idleTimeSeconds;
        set
        {
            if (SetProperty(ref _idleTimeSeconds, value))
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

    // Monday properties
    public bool MondayEnabled
    {
        get => _mondayEnabled;
        set
        {
            if (SetProperty(ref _mondayEnabled, value))
            {
                OnPropertyChanged(nameof(MondayTimeEnabled));
            }
        }
    }

    public bool MondayAllDay
    {
        get => _mondayAllDay;
        set
        {
            if (SetProperty(ref _mondayAllDay, value))
            {
                OnPropertyChanged(nameof(MondayTimeEnabled));
            }
        }
    }

    public string MondayStartText
    {
        get => _mondayStartText;
        set => SetProperty(ref _mondayStartText, value);
    }

    public string MondayEndText
    {
        get => _mondayEndText;
        set => SetProperty(ref _mondayEndText, value);
    }

    public bool MondayTimeEnabled => MondayEnabled && !MondayAllDay;

    // Tuesday properties
    public bool TuesdayEnabled
    {
        get => _tuesdayEnabled;
        set
        {
            if (SetProperty(ref _tuesdayEnabled, value))
            {
                OnPropertyChanged(nameof(TuesdayTimeEnabled));
            }
        }
    }

    public bool TuesdayAllDay
    {
        get => _tuesdayAllDay;
        set
        {
            if (SetProperty(ref _tuesdayAllDay, value))
            {
                OnPropertyChanged(nameof(TuesdayTimeEnabled));
            }
        }
    }

    public string TuesdayStartText
    {
        get => _tuesdayStartText;
        set => SetProperty(ref _tuesdayStartText, value);
    }

    public string TuesdayEndText
    {
        get => _tuesdayEndText;
        set => SetProperty(ref _tuesdayEndText, value);
    }

    public bool TuesdayTimeEnabled => TuesdayEnabled && !TuesdayAllDay;

    // Wednesday properties
    public bool WednesdayEnabled
    {
        get => _wednesdayEnabled;
        set
        {
            if (SetProperty(ref _wednesdayEnabled, value))
            {
                OnPropertyChanged(nameof(WednesdayTimeEnabled));
            }
        }
    }

    public bool WednesdayAllDay
    {
        get => _wednesdayAllDay;
        set
        {
            if (SetProperty(ref _wednesdayAllDay, value))
            {
                OnPropertyChanged(nameof(WednesdayTimeEnabled));
            }
        }
    }

    public string WednesdayStartText
    {
        get => _wednesdayStartText;
        set => SetProperty(ref _wednesdayStartText, value);
    }

    public string WednesdayEndText
    {
        get => _wednesdayEndText;
        set => SetProperty(ref _wednesdayEndText, value);
    }

    public bool WednesdayTimeEnabled => WednesdayEnabled && !WednesdayAllDay;

    // Thursday properties
    public bool ThursdayEnabled
    {
        get => _thursdayEnabled;
        set
        {
            if (SetProperty(ref _thursdayEnabled, value))
            {
                OnPropertyChanged(nameof(ThursdayTimeEnabled));
            }
        }
    }

    public bool ThursdayAllDay
    {
        get => _thursdayAllDay;
        set
        {
            if (SetProperty(ref _thursdayAllDay, value))
            {
                OnPropertyChanged(nameof(ThursdayTimeEnabled));
            }
        }
    }

    public string ThursdayStartText
    {
        get => _thursdayStartText;
        set => SetProperty(ref _thursdayStartText, value);
    }

    public string ThursdayEndText
    {
        get => _thursdayEndText;
        set => SetProperty(ref _thursdayEndText, value);
    }

    public bool ThursdayTimeEnabled => ThursdayEnabled && !ThursdayAllDay;

    // Friday properties
    public bool FridayEnabled
    {
        get => _fridayEnabled;
        set
        {
            if (SetProperty(ref _fridayEnabled, value))
            {
                OnPropertyChanged(nameof(FridayTimeEnabled));
            }
        }
    }

    public bool FridayAllDay
    {
        get => _fridayAllDay;
        set
        {
            if (SetProperty(ref _fridayAllDay, value))
            {
                OnPropertyChanged(nameof(FridayTimeEnabled));
            }
        }
    }

    public string FridayStartText
    {
        get => _fridayStartText;
        set => SetProperty(ref _fridayStartText, value);
    }

    public string FridayEndText
    {
        get => _fridayEndText;
        set => SetProperty(ref _fridayEndText, value);
    }

    public bool FridayTimeEnabled => FridayEnabled && !FridayAllDay;

    // Saturday properties
    public bool SaturdayEnabled
    {
        get => _saturdayEnabled;
        set
        {
            if (SetProperty(ref _saturdayEnabled, value))
            {
                OnPropertyChanged(nameof(SaturdayTimeEnabled));
            }
        }
    }

    public bool SaturdayAllDay
    {
        get => _saturdayAllDay;
        set
        {
            if (SetProperty(ref _saturdayAllDay, value))
            {
                OnPropertyChanged(nameof(SaturdayTimeEnabled));
            }
        }
    }

    public string SaturdayStartText
    {
        get => _saturdayStartText;
        set => SetProperty(ref _saturdayStartText, value);
    }

    public string SaturdayEndText
    {
        get => _saturdayEndText;
        set => SetProperty(ref _saturdayEndText, value);
    }

    public bool SaturdayTimeEnabled => SaturdayEnabled && !SaturdayAllDay;

    // Sunday properties
    public bool SundayEnabled
    {
        get => _sundayEnabled;
        set
        {
            if (SetProperty(ref _sundayEnabled, value))
            {
                OnPropertyChanged(nameof(SundayTimeEnabled));
            }
        }
    }

    public bool SundayAllDay
    {
        get => _sundayAllDay;
        set
        {
            if (SetProperty(ref _sundayAllDay, value))
            {
                OnPropertyChanged(nameof(SundayTimeEnabled));
            }
        }
    }

    public string SundayStartText
    {
        get => _sundayStartText;
        set => SetProperty(ref _sundayStartText, value);
    }

    public string SundayEndText
    {
        get => _sundayEndText;
        set => SetProperty(ref _sundayEndText, value);
    }

    public bool SundayTimeEnabled => SundayEnabled && !SundayAllDay;

    // Schedule mode properties
    public ScheduleMode ScheduleMode
    {
        get => _scheduleMode;
        set
        {
            if (SetProperty(ref _scheduleMode, value))
            {
                OnPropertyChanged(nameof(DailyModeVisible));
                OnPropertyChanged(nameof(WeeklyModeVisible));
            }
        }
    }

    public string DailyStartText
    {
        get => _dailyStartText;
        set => SetProperty(ref _dailyStartText, value);
    }

    public string DailyEndText
    {
        get => _dailyEndText;
        set => SetProperty(ref _dailyEndText, value);
    }

    public Visibility DailyModeVisible => ScheduleMode == ScheduleMode.Daily ? Visibility.Visible : Visibility.Collapsed;
    public Visibility WeeklyModeVisible => ScheduleMode == ScheduleMode.Weekly ? Visibility.Visible : Visibility.Collapsed;

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    public PowerAction PowerAction
    {
        get => _powerAction;
        set => SetProperty(ref _powerAction, value);
    }

    public bool ShowConfirmationDialog
    {
        get => _showConfirmationDialog;
        set => SetProperty(ref _showConfirmationDialog, value);
    }

    public int ConfirmationCountdownSeconds
    {
        get => _confirmationCountdownSeconds;
        set => SetProperty(ref _confirmationCountdownSeconds, value);
    }

    public bool EnableSleepLogging
    {
        get => _enableSleepLogging;
        set => SetProperty(ref _enableSleepLogging, value);
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

    public Brush LiveInputBrush
    {
        get => _lastSnapshot?.InputActivityDetected == true ? Brushes.Orange : Brushes.Black;
    }

    public Brush LiveCpuBrush
    {
        get => _lastSnapshot?.CpuExceeding == true ? Brushes.Orange : Brushes.Black;
    }

    public Brush LiveNetworkBrush
    {
        get => _lastSnapshot?.NetworkExceeding == true ? Brushes.Orange : Brushes.Black;
    }

    public AppLanguage Language
    {
        get => _language;
        set
        {
            if (SetProperty(ref _language, value))
            {
                LocalizationManager.SetLanguage(value);
                RefreshLiveStatus();
            }
        }
    }


    public static SettingsViewModel FromConfig(AppConfig config)
    {
        var cooldown = config.SleepCooldownSeconds >= 10 ? config.SleepCooldownSeconds : 45;

        return new SettingsViewModel
        {
            UseInputActivity = config.Idle.UseInputActivity,
            IncludeGamepadInput = config.Idle.IncludeGamepadInput,
            UseCpuActivity = config.Idle.UseCpuActivity,
            CpuUsageThreshold = config.Idle.CpuUsagePercentageThreshold,
            CpuSmoothingWindow = config.Idle.CpuSmoothingWindow,
            UseNetworkActivity = config.Idle.UseNetworkActivity,
            NetworkThreshold = config.Idle.NetworkKilobytesPerSecondThreshold,
            NetworkSmoothingWindow = config.Idle.NetworkSmoothingWindow,
            IdleTimeSeconds = config.Idle.IdleTimeSeconds,
            PollingIntervalSeconds = config.PollingIntervalSeconds,
            // Schedule mode
            ScheduleMode = config.Schedule.Mode,
            DailyStartText = config.Schedule.DailyStartTime.ToString("hh\\:mm"),
            DailyEndText = config.Schedule.DailyEndTime.ToString("hh\\:mm"),
            // Weekly schedule
            MondayEnabled = config.Schedule.Monday.Enabled,
            MondayAllDay = config.Schedule.Monday.AllDay,
            MondayStartText = config.Schedule.Monday.StartTime.ToString("hh\\:mm"),
            MondayEndText = config.Schedule.Monday.EndTime.ToString("hh\\:mm"),

            TuesdayEnabled = config.Schedule.Tuesday.Enabled,
            TuesdayAllDay = config.Schedule.Tuesday.AllDay,
            TuesdayStartText = config.Schedule.Tuesday.StartTime.ToString("hh\\:mm"),
            TuesdayEndText = config.Schedule.Tuesday.EndTime.ToString("hh\\:mm"),

            WednesdayEnabled = config.Schedule.Wednesday.Enabled,
            WednesdayAllDay = config.Schedule.Wednesday.AllDay,
            WednesdayStartText = config.Schedule.Wednesday.StartTime.ToString("hh\\:mm"),
            WednesdayEndText = config.Schedule.Wednesday.EndTime.ToString("hh\\:mm"),

            ThursdayEnabled = config.Schedule.Thursday.Enabled,
            ThursdayAllDay = config.Schedule.Thursday.AllDay,
            ThursdayStartText = config.Schedule.Thursday.StartTime.ToString("hh\\:mm"),
            ThursdayEndText = config.Schedule.Thursday.EndTime.ToString("hh\\:mm"),

            FridayEnabled = config.Schedule.Friday.Enabled,
            FridayAllDay = config.Schedule.Friday.AllDay,
            FridayStartText = config.Schedule.Friday.StartTime.ToString("hh\\:mm"),
            FridayEndText = config.Schedule.Friday.EndTime.ToString("hh\\:mm"),

            SaturdayEnabled = config.Schedule.Saturday.Enabled,
            SaturdayAllDay = config.Schedule.Saturday.AllDay,
            SaturdayStartText = config.Schedule.Saturday.StartTime.ToString("hh\\:mm"),
            SaturdayEndText = config.Schedule.Saturday.EndTime.ToString("hh\\:mm"),

            SundayEnabled = config.Schedule.Sunday.Enabled,
            SundayAllDay = config.Schedule.Sunday.AllDay,
            SundayStartText = config.Schedule.Sunday.StartTime.ToString("hh\\:mm"),
            SundayEndText = config.Schedule.Sunday.EndTime.ToString("hh\\:mm"),
            StartWithWindows = config.StartWithWindows,
            PowerAction = config.PowerAction,
            ShowConfirmationDialog = config.ShowConfirmationDialog,
            ConfirmationCountdownSeconds = config.ConfirmationCountdownSeconds,
            EnableSleepLogging = config.EnableSleepLogging,
            SleepCooldownSeconds = cooldown,
            Language = config.Language
        };
    }

    public bool TryValidate(out string validationError)
    {
        if (IdleTimeSeconds < 0)
        {
            validationError = "Idle time must be 0 or greater.";
            return false;
        }

        if (UseCpuActivity)
        {
            if (CpuUsageThreshold < 0 || CpuUsageThreshold > 100)
            {
                validationError = "CPU threshold must be between 0 and 100.";
                return false;
            }
        }

        if (CpuSmoothingWindow <= 0)
        {
            validationError = "CPU smoothing window must be 1 or greater.";
            return false;
        }

        if (UseNetworkActivity)
        {
            if (NetworkThreshold < 0)
            {
                validationError = "Network threshold must be 0 or greater.";
                return false;
            }
        }

        if (NetworkSmoothingWindow <= 0)
        {
            validationError = "Network smoothing window must be 1 or greater.";
            return false;
        }

        if (SleepCooldownSeconds < 10)
        {
            validationError = "Sleep cooldown must be 10 seconds or greater.";
            return false;
        }

        // Validate schedule time formats based on mode
        if (ScheduleMode == ScheduleMode.Daily)
        {
            if (!TimeSpan.TryParse(DailyStartText, out var dailyStart) || !TimeSpan.TryParse(DailyEndText, out var dailyEnd))
            {
                validationError = "Daily monitoring time must be in HH:mm format.";
                return false;
            }

            if (dailyStart == TimeSpan.Zero && dailyEnd == TimeSpan.Zero)
            {
                validationError = "Daily monitoring time cannot be 00:00 ~ 00:00. Use 'Disabled' mode instead.";
                return false;
            }
        }
        else if (ScheduleMode == ScheduleMode.Weekly)
        {
            var timeTexts = new[]
            {
                (MondayEnabled && !MondayAllDay, MondayStartText, MondayEndText, "월요일"),
                (TuesdayEnabled && !TuesdayAllDay, TuesdayStartText, TuesdayEndText, "화요일"),
                (WednesdayEnabled && !WednesdayAllDay, WednesdayStartText, WednesdayEndText, "수요일"),
                (ThursdayEnabled && !ThursdayAllDay, ThursdayStartText, ThursdayEndText, "목요일"),
                (FridayEnabled && !FridayAllDay, FridayStartText, FridayEndText, "금요일"),
                (SaturdayEnabled && !SaturdayAllDay, SaturdayStartText, SaturdayEndText, "토요일"),
                (SundayEnabled && !SundayAllDay, SundayStartText, SundayEndText, "일요일")
            };

            foreach (var (enabled, startText, endText, dayName) in timeTexts)
            {
                if (enabled)
                {
                    if (!TimeSpan.TryParse(startText, out var start) || !TimeSpan.TryParse(endText, out var end))
                    {
                        validationError = $"{dayName} monitoring time must be in HH:mm format.";
                        return false;
                    }

                    if (start == TimeSpan.Zero && end == TimeSpan.Zero)
                    {
                        validationError = $"{dayName} monitoring time cannot be 00:00 ~ 00:00. Use 'All Day' or disable the day instead.";
                        return false;
                    }
                }
            }
        }

        if (PollingIntervalSeconds <= 0)
        {
            validationError = "Monitoring interval must be greater than 0.";
            return false;
        }

        validationError = string.Empty;
        return true;
    }

    public AppConfig ToConfig(AppConfig existing)
    {
        var config = existing.Clone();
        config.Idle.UseInputActivity = UseInputActivity;
        config.Idle.IncludeGamepadInput = IncludeGamepadInput;
        config.Idle.UseCpuActivity = UseCpuActivity;
        config.Idle.CpuUsagePercentageThreshold = CpuUsageThreshold;
        config.Idle.CpuSmoothingWindow = CpuSmoothingWindow;
        config.Idle.UseNetworkActivity = UseNetworkActivity;
        config.Idle.NetworkKilobytesPerSecondThreshold = NetworkThreshold;
        config.Idle.NetworkSmoothingWindow = NetworkSmoothingWindow;
        config.Idle.IdleTimeSeconds = IdleTimeSeconds;
        config.PollingIntervalSeconds = PollingIntervalSeconds;
        config.StartWithWindows = StartWithWindows;
        config.PowerAction = PowerAction;
        config.ShowConfirmationDialog = ShowConfirmationDialog;
        config.ConfirmationCountdownSeconds = Math.Max(1, ConfirmationCountdownSeconds);
        config.EnableSleepLogging = EnableSleepLogging;
        config.SleepCooldownSeconds = Math.Max(10, SleepCooldownSeconds);
        config.Language = Language;

        // Schedule mode
        config.Schedule.Mode = ScheduleMode;
        if (TimeSpan.TryParse(DailyStartText, out var dailyStart))
        {
            config.Schedule.DailyStartTime = dailyStart;
        }
        if (TimeSpan.TryParse(DailyEndText, out var dailyEnd))
        {
            config.Schedule.DailyEndTime = dailyEnd;
        }

        // Monday
        config.Schedule.Monday.Enabled = MondayEnabled;
        config.Schedule.Monday.AllDay = MondayAllDay;
        if (TimeSpan.TryParse(MondayStartText, out var mondayStart))
        {
            config.Schedule.Monday.StartTime = mondayStart;
        }
        if (TimeSpan.TryParse(MondayEndText, out var mondayEnd))
        {
            config.Schedule.Monday.EndTime = mondayEnd;
        }

        // Tuesday
        config.Schedule.Tuesday.Enabled = TuesdayEnabled;
        config.Schedule.Tuesday.AllDay = TuesdayAllDay;
        if (TimeSpan.TryParse(TuesdayStartText, out var tuesdayStart))
        {
            config.Schedule.Tuesday.StartTime = tuesdayStart;
        }
        if (TimeSpan.TryParse(TuesdayEndText, out var tuesdayEnd))
        {
            config.Schedule.Tuesday.EndTime = tuesdayEnd;
        }

        // Wednesday
        config.Schedule.Wednesday.Enabled = WednesdayEnabled;
        config.Schedule.Wednesday.AllDay = WednesdayAllDay;
        if (TimeSpan.TryParse(WednesdayStartText, out var wednesdayStart))
        {
            config.Schedule.Wednesday.StartTime = wednesdayStart;
        }
        if (TimeSpan.TryParse(WednesdayEndText, out var wednesdayEnd))
        {
            config.Schedule.Wednesday.EndTime = wednesdayEnd;
        }

        // Thursday
        config.Schedule.Thursday.Enabled = ThursdayEnabled;
        config.Schedule.Thursday.AllDay = ThursdayAllDay;
        if (TimeSpan.TryParse(ThursdayStartText, out var thursdayStart))
        {
            config.Schedule.Thursday.StartTime = thursdayStart;
        }
        if (TimeSpan.TryParse(ThursdayEndText, out var thursdayEnd))
        {
            config.Schedule.Thursday.EndTime = thursdayEnd;
        }

        // Friday
        config.Schedule.Friday.Enabled = FridayEnabled;
        config.Schedule.Friday.AllDay = FridayAllDay;
        if (TimeSpan.TryParse(FridayStartText, out var fridayStart))
        {
            config.Schedule.Friday.StartTime = fridayStart;
        }
        if (TimeSpan.TryParse(FridayEndText, out var fridayEnd))
        {
            config.Schedule.Friday.EndTime = fridayEnd;
        }

        // Saturday
        config.Schedule.Saturday.Enabled = SaturdayEnabled;
        config.Schedule.Saturday.AllDay = SaturdayAllDay;
        if (TimeSpan.TryParse(SaturdayStartText, out var saturdayStart))
        {
            config.Schedule.Saturday.StartTime = saturdayStart;
        }
        if (TimeSpan.TryParse(SaturdayEndText, out var saturdayEnd))
        {
            config.Schedule.Saturday.EndTime = saturdayEnd;
        }

        // Sunday
        config.Schedule.Sunday.Enabled = SundayEnabled;
        config.Schedule.Sunday.AllDay = SundayAllDay;
        if (TimeSpan.TryParse(SundayStartText, out var sundayStart))
        {
            config.Schedule.Sunday.StartTime = sundayStart;
        }
        if (TimeSpan.TryParse(SundayEndText, out var sundayEnd))
        {
            config.Schedule.Sunday.EndTime = sundayEnd;
        }
        return config;
    }

    public void UpdateLiveSnapshot(MonitoringSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        RefreshLiveStatus();
    }

    public void RefreshLiveStatus()
    {
        if (_lastSnapshot == null)
        {
            LiveInputStatus = LocalizationManager.GetString("LiveStatus_InputWaiting");
            LiveCpuStatus = string.Empty;
            LiveNetworkStatus = string.Empty;
            LiveStatusMessage = string.Empty;
            LiveStatusBrush = Brushes.SlateGray;
            return;
        }

        var snapshot = _lastSnapshot;

        // Update individual status lines (simplified format without numbers)
        if (snapshot.InputMonitoringEnabled)
        {
            LiveInputStatus = snapshot.InputActivityDetected
                ? LocalizationManager.GetString("LiveStatus_InputActive") // "입력: 활동 감지됨"
                : LocalizationManager.GetString("LiveStatus_InputWaiting"); // "입력: 대기중"
        }
        else
        {
            LiveInputStatus = LocalizationManager.GetString("LiveStatus_InputDisabled"); // "입력: 비활성화"
        }

        if (snapshot.CpuMonitoringEnabled)
        {
            LiveCpuStatus = LocalizationManager.Format("LiveStatus_Cpu", snapshot.CpuUsagePercent, snapshot.CpuThresholdPercent);
        }
        else
        {
            LiveCpuStatus = LocalizationManager.Format("LiveStatus_CpuDisabled", snapshot.CpuUsagePercent);
        }

        if (snapshot.NetworkMonitoringEnabled)
        {
            LiveNetworkStatus = LocalizationManager.Format("LiveStatus_Network", snapshot.NetworkKilobytesPerSecond, snapshot.NetworkThresholdKilobytesPerSecond);
        }
        else
        {
            LiveNetworkStatus = LocalizationManager.Format("LiveStatus_NetworkDisabled", snapshot.NetworkKilobytesPerSecond);
        }

        // Conditions count display removed - no longer needed with real-time activity detection

        // Main status message - already contains the last-changed message from MonitoringService
        var (displayText, brush) = StatusDisplayHelper.GetStatusMessage(
            snapshot.StatusMessage,
            snapshot.InputActivityDetected,
            snapshot.CpuExceeding,
            snapshot.NetworkExceeding,
            forTooltip: false);

        LiveStatusMessage = displayText;
        LiveStatusBrush = brush;

        // Notify property changes for brushes
        OnPropertyChanged(nameof(LiveInputBrush));
        OnPropertyChanged(nameof(LiveCpuBrush));
        OnPropertyChanged(nameof(LiveNetworkBrush));

        _previousSnapshot = snapshot;
    }



}
