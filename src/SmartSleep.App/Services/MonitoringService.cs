using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSleep.App.Models;
using SmartSleep.App.Utilities;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.Services;

public class MonitoringService : IDisposable
{
    private readonly SleepService _sleepService;
    private readonly SleepLogService _logService = new();
    private readonly CpuUsageSampler _cpuSampler = new();
    private readonly NetworkUsageSampler _networkSampler = new();
    private readonly object _configLock = new();
    private AppConfig _config = AppConfig.CreateDefault();
    private CancellationTokenSource? _cts;
    private Task? _monitoringTask;
    private DateTime _inputIdleBaselineUtc = DateTime.UtcNow;
    private DateTime _lastCpuActiveUtc = DateTime.UtcNow;
    private DateTime _lastNetworkActiveUtc = DateTime.UtcNow;
    private DateTime _lastSleepAttemptUtc = DateTime.MinValue;
    private DateTime? _lastSleepSuccessUtc;
    private TimeSpan _sleepCooldown = TimeSpan.FromSeconds(DefaultValues.SleepCooldownSeconds);
    private bool _previousInputActivityDetected;
    private bool _previousCpuExceeding;
    private bool _previousNetworkExceeding;
    private string? _lastChangeBasedStatusMessage;

    public event EventHandler<MonitoringSnapshot>? SnapshotAvailable;
    public event EventHandler<string>? SleepTriggered;

    public MonitoringSnapshot? LastSnapshot { get; private set; }

    public MonitoringService(SleepService sleepService)
    {
        _sleepService = sleepService;
        InputActivityReader.GamepadConnectionChanged += OnGamepadConnectionChanged;
    }

    public void UpdateConfiguration(AppConfig config)
    {
        lock (_configLock)
        {
            var now = DateTime.UtcNow;
            var previous = _config;
            _config = config.Clone();

            if (!previous.Idle.UseCpuActivity && _config.Idle.UseCpuActivity)
            {
                _lastCpuActiveUtc = now;
            }

            if (!previous.Idle.UseNetworkActivity && _config.Idle.UseNetworkActivity)
            {
                _lastNetworkActiveUtc = now;
            }

            _cpuSampler.SetWindowSize(_config.Idle.CpuSmoothingWindow);
            _networkSampler.SetWindowSize(_config.Idle.NetworkSmoothingWindow);
            ApplySleepCooldownFromConfig();
        }
    }

    public void Start()
    {
        if (_monitoringTask is { IsCompleted: false })
        {
            return;
        }

        var now = DateTime.UtcNow;
        _inputIdleBaselineUtc = now;
        _lastCpuActiveUtc = now;
        _lastNetworkActiveUtc = now;
        _lastSleepAttemptUtc = DateTime.MinValue;

        lock (_configLock)
        {
            _cpuSampler.SetWindowSize(_config.Idle.CpuSmoothingWindow);
            _networkSampler.SetWindowSize(_config.Idle.NetworkSmoothingWindow);
            ApplySleepCooldownFromConfig();
        }

        _cts = new CancellationTokenSource();
        _monitoringTask = Task.Run(() => RunAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts == null)
        {
            return;
        }

        _cts.Cancel();
        try
        {
            if (_monitoringTask != null)
            {
                await _monitoringTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected when stopping
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _monitoringTask = null;
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            AppConfig snapshotConfig;
            lock (_configLock)
            {
                snapshotConfig = _config.Clone();
            }

            var nowUtc = DateTime.UtcNow;
            var nowLocal = nowUtc.ToLocalTime();

            var scheduleActive = snapshotConfig.Schedule.IsWithinWindow(nowLocal);

            var rawInputIdle = InputActivityReader.GetIdleTime(snapshotConfig.Idle.IncludeGamepadInput);
            var rawBaselineUtc = nowUtc - rawInputIdle;
            var inputActivityDetected = rawBaselineUtc > _inputIdleBaselineUtc;
            if (inputActivityDetected)
            {
                _inputIdleBaselineUtc = rawBaselineUtc;
            }

            var inputIdle = nowUtc - _inputIdleBaselineUtc;
            var cpuUsage = _cpuSampler.SampleCpuUsagePercentage();
            var networkUsage = _networkSampler.SampleKilobitsPerSecond();
            var cpuUsageOk = cpuUsage <= snapshotConfig.Idle.CpuUsagePercentageThreshold;
            var networkUsageOk = networkUsage <= snapshotConfig.Idle.NetworkKilobitsPerSecondThreshold;
            var cpuIdleDuration = CalculateCpuIdleDuration(snapshotConfig, cpuUsage, nowUtc);
            var networkIdleDuration = CalculateNetworkIdleDuration(snapshotConfig, networkUsage, nowUtc);

            if (!snapshotConfig.Idle.UseInputActivity)
            {
                inputActivityDetected = false;
            }

            // Detect state changes and update message accordingly (only when schedule is active)
            var cpuExceeding = snapshotConfig.Idle.UseCpuActivity && !cpuUsageOk;
            var networkExceeding = snapshotConfig.Idle.UseNetworkActivity && !networkUsageOk;

            if (scheduleActive)
            {
                if (inputActivityDetected && !_previousInputActivityDetected)
                {
                    _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_ActivityDetected");
                }
                else if (cpuExceeding && !_previousCpuExceeding)
                {
                    _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_CpuExceeded");
                }
                else if (networkExceeding && !_previousNetworkExceeding)
                {
                    _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_NetworkExceeded");
                }
                else if (!inputActivityDetected && _previousInputActivityDetected)
                {
                    // Input activity stopped, check if other conditions are still active
                    if (cpuExceeding)
                    {
                        _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_CpuExceeded");
                    }
                    else if (networkExceeding)
                    {
                        _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_NetworkExceeded");
                    }
                    else
                    {
                        _lastChangeBasedStatusMessage = null;
                    }
                }
                else if (!cpuExceeding && _previousCpuExceeding)
                {
                    // CPU activity stopped, check if other conditions are still active
                    if (inputActivityDetected)
                    {
                        _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_ActivityDetected");
                    }
                    else if (networkExceeding)
                    {
                        _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_NetworkExceeded");
                    }
                    else
                    {
                        _lastChangeBasedStatusMessage = null;
                    }
                }
                else if (!networkExceeding && _previousNetworkExceeding)
                {
                    // Network activity stopped, check if other conditions are still active
                    if (inputActivityDetected)
                    {
                        _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_ActivityDetected");
                    }
                    else if (cpuExceeding)
                    {
                        _lastChangeBasedStatusMessage = LocalizationManager.GetString("Status_CpuExceeded");
                    }
                    else
                    {
                        _lastChangeBasedStatusMessage = null;
                    }
                }
            }
            else
            {
                // Schedule not active, clear any existing change-based status message
                _lastChangeBasedStatusMessage = null;
            }

            _previousInputActivityDetected = inputActivityDetected;
            _previousCpuExceeding = cpuExceeding;
            _previousNetworkExceeding = networkExceeding;

            var shouldPropagateReset =
                scheduleActive &&
                (inputActivityDetected || cpuExceeding || networkExceeding);

            if (!scheduleActive || shouldPropagateReset)
            {
                ResetIdleCounters(nowUtc);
                inputIdle = TimeSpan.Zero;
                cpuIdleDuration = TimeSpan.Zero;
                networkIdleDuration = TimeSpan.Zero;
            }

            var inputConditionMet = !snapshotConfig.Idle.UseInputActivity || inputIdle >= snapshotConfig.Idle.InputIdleThreshold;
            var cpuDurationMet = cpuIdleDuration >= snapshotConfig.Idle.CpuIdleDurationRequirement;
            var cpuConditionMet = !snapshotConfig.Idle.UseCpuActivity || (cpuUsageOk && cpuDurationMet);
            var networkDurationMet = networkIdleDuration >= snapshotConfig.Idle.NetworkIdleDurationRequirement;
            var networkConditionMet = !snapshotConfig.Idle.UseNetworkActivity || (networkUsageOk && networkDurationMet);

            var enabledCount = CountEnabledConditions(snapshotConfig);
            var satisfiedCount = CountSatisfiedConditions(snapshotConfig, inputConditionMet, cpuConditionMet, networkConditionMet);
            var combinationSatisfied = EvaluateCombination(enabledCount, satisfiedCount);

            var inputIdleDisplay = (!scheduleActive || !snapshotConfig.Idle.UseInputActivity) ? TimeSpan.Zero : inputIdle;
            var cpuIdleDisplay = (scheduleActive && snapshotConfig.Idle.UseCpuActivity) ? cpuIdleDuration : TimeSpan.Zero;
            var networkIdleDisplay = (scheduleActive && snapshotConfig.Idle.UseNetworkActivity) ? networkIdleDuration : TimeSpan.Zero;

            var cooldownRemainingSeconds = GetCooldownRemainingSeconds(nowUtc);

            var statusMessage = _lastChangeBasedStatusMessage ?? DetermineStatusMessage(snapshotConfig,
                nowUtc,
                scheduleActive,
                inputIdle,
                inputConditionMet,
                cpuUsage,
                cpuUsageOk,
                cpuIdleDuration,
                cpuDurationMet,
                networkUsage,
                networkUsageOk,
                networkIdleDuration,
                networkDurationMet,
                enabledCount,
                satisfiedCount,
                combinationSatisfied,
                cooldownRemainingSeconds);

            var conditionsMet = scheduleActive && combinationSatisfied;

            if (conditionsMet && CanAttemptSleep(nowUtc))
            {
                TriggerSleep();
                _lastCpuActiveUtc = nowUtc;
                _lastNetworkActiveUtc = nowUtc;
                _lastSleepAttemptUtc = nowUtc;
                statusMessage = LocalizationManager.Format("Status_ActionNow", GetActionText(snapshotConfig.PowerAction));
            }

            var snapshot = new MonitoringSnapshot
            {
                Timestamp = nowLocal,
                InputMonitoringEnabled = scheduleActive && snapshotConfig.Idle.UseInputActivity,
                InputIdle = inputIdleDisplay,
                InputIdleRequirement = snapshotConfig.Idle.InputIdleThreshold,
                InputConditionMet = inputConditionMet,
                CpuMonitoringEnabled = scheduleActive && snapshotConfig.Idle.UseCpuActivity,
                CpuUsagePercent = cpuUsage,
                CpuThresholdPercent = snapshotConfig.Idle.CpuUsagePercentageThreshold,
                CpuIdleDuration = cpuIdleDisplay,
                CpuIdleRequirement = snapshotConfig.Idle.CpuIdleDurationRequirement,
                CpuConditionMet = cpuConditionMet,
                NetworkMonitoringEnabled = scheduleActive && snapshotConfig.Idle.UseNetworkActivity,
                NetworkKilobitsPerSecond = networkUsage,
                NetworkThresholdKilobitsPerSecond = snapshotConfig.Idle.NetworkKilobitsPerSecondThreshold,
                NetworkIdleDuration = networkIdleDisplay,
                NetworkIdleRequirement = snapshotConfig.Idle.NetworkIdleDurationRequirement,
                NetworkConditionMet = networkConditionMet,
                EnabledConditionCount = enabledCount,
                SatisfiedConditionCount = satisfiedCount,
                ScheduleActive = scheduleActive,
                ConditionsMet = conditionsMet,
                StatusMessage = statusMessage,
                InputActivityDetected = inputActivityDetected,
                CpuExceeding = cpuExceeding,
                NetworkExceeding = networkExceeding
            };

            LastSnapshot = snapshot;
            SnapshotAvailable?.Invoke(this, snapshot);

            var delaySeconds = Math.Max(1, snapshotConfig.PollingIntervalSeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void ResetIdleCounters(DateTime nowUtc)
    {
        _inputIdleBaselineUtc = nowUtc;
        _lastCpuActiveUtc = nowUtc;
        _lastNetworkActiveUtc = nowUtc;
    }

    private TimeSpan CalculateCpuIdleDuration(AppConfig config, double cpuUsage, DateTime nowUtc)
    {
        if (!config.Idle.UseCpuActivity)
        {
            _lastCpuActiveUtc = nowUtc;
            return TimeSpan.Zero;
        }

        if (cpuUsage <= config.Idle.CpuUsagePercentageThreshold)
        {
            return nowUtc - _lastCpuActiveUtc;
        }

        _lastCpuActiveUtc = nowUtc;
        return TimeSpan.Zero;
    }

    private TimeSpan CalculateNetworkIdleDuration(AppConfig config, double networkUsage, DateTime nowUtc)
    {
        if (!config.Idle.UseNetworkActivity)
        {
            _lastNetworkActiveUtc = nowUtc;
            return TimeSpan.Zero;
        }

        if (networkUsage <= config.Idle.NetworkKilobitsPerSecondThreshold)
        {
            return nowUtc - _lastNetworkActiveUtc;
        }

        _lastNetworkActiveUtc = nowUtc;
        return TimeSpan.Zero;
    }

    private static int CountEnabledConditions(AppConfig config)
    {
        var count = 0;
        if (config.Idle.UseInputActivity)
        {
            count++;
        }

        if (config.Idle.UseCpuActivity)
        {
            count++;
        }

        if (config.Idle.UseNetworkActivity)
        {
            count++;
        }

        return count;
    }

    private static int CountSatisfiedConditions(AppConfig config, bool inputMet, bool cpuMet, bool networkMet)
    {
        var count = 0;
        if (config.Idle.UseInputActivity && inputMet)
        {
            count++;
        }

        if (config.Idle.UseCpuActivity && cpuMet)
        {
            count++;
        }

        if (config.Idle.UseNetworkActivity && networkMet)
        {
            count++;
        }

        return count;
    }

    private static bool EvaluateCombination(int enabledCount, int satisfiedCount)
    {
        if (enabledCount == 0)
        {
            return false;
        }

        return satisfiedCount == enabledCount;
    }

    private string DetermineStatusMessage(
        AppConfig config,
        DateTime nowUtc,
        bool scheduleActive,
        TimeSpan inputIdle,
        bool inputConditionMet,
        double cpuUsage,
        bool cpuUsageOk,
        TimeSpan cpuIdleDuration,
        bool cpuDurationMet,
        double networkUsage,
        bool networkUsageOk,
        TimeSpan networkIdleDuration,
        bool networkDurationMet,
        int enabledCount,
        int satisfiedCount,
        bool combinationSatisfied,
        double cooldownRemainingSeconds)
    {
        if (enabledCount == 0)
        {
            return LocalizationManager.GetString("Status_NoConditions");
        }

        var actionText = GetActionText(config.PowerAction);

        var nowLocal = nowUtc.ToLocalTime();
        var scheduleRemaining = GetScheduleRemainingSeconds(config.Schedule, nowLocal);

        if (!scheduleActive && HasAnyActiveSchedule(config.Schedule))
        {
            if (scheduleRemaining == double.MaxValue)
            {
                return LocalizationManager.GetString("Status_MonitoringDisabled");
            }
            return LocalizationManager.Format("Status_NextSchedule", Math.Ceiling(Math.Min(scheduleRemaining, 999999)));
        }

        double inputRemaining = config.Idle.UseInputActivity
            ? Math.Max(0, config.Idle.InputIdleThreshold.TotalSeconds - inputIdle.TotalSeconds)
            : 0;

        double cpuRemaining = config.Idle.UseCpuActivity
            ? Math.Max(0, config.Idle.CpuIdleDurationRequirement.TotalSeconds - cpuIdleDuration.TotalSeconds)
            : 0;

        double networkRemaining = config.Idle.UseNetworkActivity
            ? Math.Max(0, config.Idle.NetworkIdleDurationRequirement.TotalSeconds - networkIdleDuration.TotalSeconds)
            : 0;

        if (combinationSatisfied)
        {
            // If schedule is not active, don't show action countdown
            if (!scheduleActive && HasAnyActiveSchedule(config.Schedule))
            {
                return LocalizationManager.GetString("Status_NotMonitoring");
            }

            var remaining = Math.Max(scheduleRemaining, cooldownRemainingSeconds);
            if (remaining == double.MaxValue)
            {
                return LocalizationManager.GetString("Status_MonitoringDisabled");
            }
            if (remaining <= 0)
            {
                return LocalizationManager.Format("Status_ActionNow", actionText);
            }

            return LocalizationManager.Format("Status_ActionIn", actionText, Math.Ceiling(remaining));
        }

        bool cpuBlocksAll = config.Idle.UseCpuActivity && !cpuUsageOk &&
                             (!config.Idle.UseInputActivity && !config.Idle.UseNetworkActivity);
        if (cpuBlocksAll)
        {
            return LocalizationManager.Format("Status_CpuBlocking", cpuUsage, config.Idle.CpuUsagePercentageThreshold);
        }

        bool networkBlocksAll = config.Idle.UseNetworkActivity && !networkUsageOk &&
                                (!config.Idle.UseInputActivity && !config.Idle.UseCpuActivity);
        if (networkBlocksAll)
        {
            return LocalizationManager.Format("Status_NetworkBlocking", networkUsage, config.Idle.NetworkKilobitsPerSecondThreshold);
        }

        var conditionRemaining = Math.Max(inputRemaining, Math.Max(cpuRemaining, networkRemaining));

        // If schedule is not active and we have active schedules, show not monitoring
        if (!scheduleActive && HasAnyActiveSchedule(config.Schedule) && conditionRemaining <= 0)
        {
            return LocalizationManager.GetString("Status_NotMonitoring");
        }

        var totalRemaining = Math.Max(scheduleRemaining, conditionRemaining);
        totalRemaining = Math.Max(totalRemaining, cooldownRemainingSeconds);

        if (totalRemaining == double.MaxValue)
        {
            return LocalizationManager.GetString("Status_MonitoringDisabled");
        }

        return LocalizationManager.Format("Status_ActionIn", actionText, Math.Ceiling(Math.Max(0, totalRemaining)));
    }

    private double GetCooldownRemainingSeconds(DateTime nowUtc)
    {
        if (_lastSleepAttemptUtc == DateTime.MinValue)
        {
            return 0;
        }

        var remaining = (_sleepCooldown - (nowUtc - _lastSleepAttemptUtc)).TotalSeconds;
        return remaining > 0 ? remaining : 0;
    }

    private static bool HasAnyActiveSchedule(ScheduleSettings schedule)
    {
        return schedule.Mode switch
        {
            ScheduleMode.Always => false, // Always mode means no schedule restrictions
            ScheduleMode.Daily => true,   // Daily mode has schedule restrictions
            ScheduleMode.Weekly => schedule.Monday.Enabled || schedule.Tuesday.Enabled ||
                                  schedule.Wednesday.Enabled || schedule.Thursday.Enabled ||
                                  schedule.Friday.Enabled || schedule.Saturday.Enabled ||
                                  schedule.Sunday.Enabled,
            ScheduleMode.Disabled => true, // Disabled mode means monitoring is disabled
            _ => false
        };
    }

    private static double GetScheduleRemainingSeconds(ScheduleSettings schedule, DateTime nowLocal)
    {
        if (schedule.IsWithinWindow(nowLocal))
        {
            return 0;
        }

        return schedule.Mode switch
        {
            ScheduleMode.Always => 0, // Always active, no remaining time
            ScheduleMode.Daily => GetDailyScheduleRemainingSeconds(schedule, nowLocal),
            ScheduleMode.Weekly => GetWeeklyScheduleRemainingSeconds(schedule, nowLocal),
            ScheduleMode.Disabled => double.MaxValue, // Never activate
            _ => 0
        };
    }

    private static double GetDailyScheduleRemainingSeconds(ScheduleSettings schedule, DateTime nowLocal)
    {
        var today = nowLocal.Date;
        var todayStart = today + schedule.DailyStartTime;
        var todayEnd = today + schedule.DailyEndTime;

        // If start time hasn't arrived today
        if (nowLocal < todayStart)
        {
            return (todayStart - nowLocal).TotalSeconds;
        }

        // If we're past today's window, check tomorrow
        if (nowLocal >= todayEnd || (schedule.DailyEndTime < schedule.DailyStartTime && nowLocal < todayEnd))
        {
            var tomorrow = today.AddDays(1);
            var tomorrowStart = tomorrow + schedule.DailyStartTime;
            return (tomorrowStart - nowLocal).TotalSeconds;
        }

        return 0;
    }

    private static double GetWeeklyScheduleRemainingSeconds(ScheduleSettings schedule, DateTime nowLocal)
    {
        // Check if any day is enabled
        bool anyDayEnabled = schedule.Monday.Enabled || schedule.Tuesday.Enabled ||
                           schedule.Wednesday.Enabled || schedule.Thursday.Enabled ||
                           schedule.Friday.Enabled || schedule.Saturday.Enabled ||
                           schedule.Sunday.Enabled;

        if (!anyDayEnabled)
        {
            return double.MaxValue; // No days enabled, never activate
        }

        // Find next active schedule window
        var currentDay = nowLocal;
        for (int i = 0; i < 7; i++)
        {
            var daySchedule = GetDaySchedule(schedule, currentDay.DayOfWeek);
            if (daySchedule.Enabled)
            {
                if (i == 0) // today
                {
                    if (!daySchedule.AllDay)
                    {
                        var todayStart = currentDay.Date + daySchedule.StartTime;
                        if (nowLocal < todayStart)
                        {
                            return (todayStart - nowLocal).TotalSeconds;
                        }
                    }
                }
                else // future day
                {
                    var futureDate = currentDay.Date + (daySchedule.AllDay ? TimeSpan.Zero : daySchedule.StartTime);
                    return (futureDate - nowLocal).TotalSeconds;
                }
            }
            currentDay = currentDay.AddDays(1);
        }

        return double.MaxValue; // No active schedule found (should not happen)
    }

    private static DaySchedule GetDaySchedule(ScheduleSettings schedule, DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => schedule.Monday,
            DayOfWeek.Tuesday => schedule.Tuesday,
            DayOfWeek.Wednesday => schedule.Wednesday,
            DayOfWeek.Thursday => schedule.Thursday,
            DayOfWeek.Friday => schedule.Friday,
            DayOfWeek.Saturday => schedule.Saturday,
            DayOfWeek.Sunday => schedule.Sunday,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private bool CanAttemptSleep(DateTime nowUtc) => GetCooldownRemainingSeconds(nowUtc) <= 0;

    private void TriggerSleep()
    {
        AppConfig currentConfig;
        lock (_configLock)
        {
            currentConfig = _config.Clone();
        }

        var attemptUtc = DateTime.UtcNow;
        var previousSuccess = _lastSleepSuccessUtc;

        var actionText = GetActionText(currentConfig.PowerAction);

        var success = _sleepService.TryExecutePowerActionWithConfirmation(
            currentConfig.PowerAction,
            currentConfig.ShowConfirmationDialog,
            currentConfig.ConfirmationCountdownSeconds,
            out var errorCode);

        // Log the sleep action if logging is enabled
        if (currentConfig.EnableSleepLogging)
        {
            if (success)
            {
                _logService.LogSleepAction(currentConfig.PowerAction, true);
            }
            else if (currentConfig.ShowConfirmationDialog && errorCode == 0)
            {
                // Don't log cancelled actions - they're not actual sleep attempts
            }
            else
            {
                var errorMessage = $"오류 코드: {errorCode}";
                _logService.LogSleepAction(currentConfig.PowerAction, false, errorMessage);
            }
        }

        if (success)
        {
            _lastSleepSuccessUtc = attemptUtc;
            var lastSuccessText = previousSuccess.HasValue
                ? previousSuccess.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : LocalizationManager.GetString("Monitoring_LastSuccessUnknown");
            SleepTriggered?.Invoke(this, LocalizationManager.Format("Notification_ActionSucceeded", actionText, lastSuccessText));
        }
        else
        {
            var lastSuccessText = _lastSleepSuccessUtc.HasValue
                ? _lastSleepSuccessUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : LocalizationManager.GetString("Monitoring_LastSuccessUnknown");

            if (currentConfig.ShowConfirmationDialog && errorCode == 0)
            {
                SleepTriggered?.Invoke(this, LocalizationManager.Format("Notification_ActionCancelled", actionText, lastSuccessText));
            }
            else
            {
                SleepTriggered?.Invoke(this, LocalizationManager.Format("Notification_ActionFailed", actionText, errorCode, lastSuccessText));
            }
        }
    }

    private void OnGamepadConnectionChanged(object? sender, GamepadConnectionEventArgs e)
    {
        var message = e.IsConnected
            ? LocalizationManager.Format("Gamepad_BalloonConnected", e.DeviceName, e.TotalConnectedCount)
            : LocalizationManager.Format("Gamepad_BalloonDisconnected", e.DeviceName, e.TotalConnectedCount);

        SleepTriggered?.Invoke(this, LocalizationManager.Format("Gamepad_BalloonPrefix", message));
    }

    public void Dispose()
    {
        InputActivityReader.GamepadConnectionChanged -= OnGamepadConnectionChanged;
        _cts?.Cancel();
        _cts?.Dispose();
        InputActivityReader.Cleanup();
    }

    private void ApplySleepCooldownFromConfig()
    {
        var seconds = Math.Max(10, _config.SleepCooldownSeconds);
        _sleepCooldown = TimeSpan.FromSeconds(seconds);
        _config.SleepCooldownSeconds = seconds;
    }

    private static string GetActionText(PowerAction powerAction)
    {
        return powerAction switch
        {
            PowerAction.Sleep => LocalizationManager.GetString("PowerAction_Sleep"),
            PowerAction.Shutdown => LocalizationManager.GetString("PowerAction_Shutdown"),
            _ => LocalizationManager.GetString("PowerAction_Generic")
        };
    }
}




