using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSleep.App.Models;
using SmartSleep.App.Utilities;

namespace SmartSleep.App.Services;

public class MonitoringService : IDisposable
{
    private readonly SleepService _sleepService;
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
    private TimeSpan _sleepCooldown = TimeSpan.FromSeconds(45);

    public event EventHandler<MonitoringSnapshot>? SnapshotAvailable;
    public event EventHandler<string>? SleepTriggered;

    public MonitoringSnapshot? LastSnapshot { get; private set; }

    public MonitoringService(SleepService sleepService)
    {
        _sleepService = sleepService;
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

            var rawInputIdle = InputActivityReader.GetIdleTime();
            var rawBaselineUtc = nowUtc - rawInputIdle;
            var inputActivityDetected = rawBaselineUtc > _inputIdleBaselineUtc;
            if (inputActivityDetected)
            {
                _inputIdleBaselineUtc = rawBaselineUtc;
            }

            var inputIdle = nowUtc - _inputIdleBaselineUtc;
            var cpuUsage = _cpuSampler.SampleCpuUsagePercentage();
            var networkUsage = _networkSampler.SampleKilobytesPerSecond();
            var cpuUsageOk = cpuUsage <= snapshotConfig.Idle.CpuUsagePercentageThreshold;
            var networkUsageOk = networkUsage <= snapshotConfig.Idle.NetworkKilobytesPerSecondThreshold;
            var cpuIdleDuration = CalculateCpuIdleDuration(snapshotConfig, cpuUsage, nowUtc);
            var networkIdleDuration = CalculateNetworkIdleDuration(snapshotConfig, networkUsage, nowUtc);

            if (!snapshotConfig.Idle.UseInputActivity)
            {
                inputActivityDetected = false;
            }

            var shouldPropagateReset =
                scheduleActive &&
                (inputActivityDetected ||
                 (snapshotConfig.Idle.UseCpuActivity && !cpuUsageOk) ||
                 (snapshotConfig.Idle.UseNetworkActivity && !networkUsageOk));

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

            var statusMessage = DetermineStatusMessage(snapshotConfig,
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
                statusMessage = "절전 요청";
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
                NetworkKilobytesPerSecond = networkUsage,
                NetworkThresholdKilobytesPerSecond = snapshotConfig.Idle.NetworkKilobytesPerSecondThreshold,
                NetworkIdleDuration = networkIdleDisplay,
                NetworkIdleRequirement = snapshotConfig.Idle.NetworkIdleDurationRequirement,
                NetworkConditionMet = networkConditionMet,
                EnabledConditionCount = enabledCount,
                SatisfiedConditionCount = satisfiedCount,
                ScheduleActive = scheduleActive,
                ConditionsMet = conditionsMet,
                StatusMessage = statusMessage
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

        if (networkUsage <= config.Idle.NetworkKilobytesPerSecondThreshold)
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
            return "활성화된 조건 없음";
        }

        var nowLocal = nowUtc.ToLocalTime();
        var scheduleRemaining = GetScheduleRemainingSeconds(config.Schedule, nowLocal);

        if (!scheduleActive && config.Schedule.Enabled)
        {
            return $"감시 시작까지 {Math.Ceiling(scheduleRemaining)}초";
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
            var remaining = Math.Max(scheduleRemaining, cooldownRemainingSeconds);
            if (remaining <= 0)
            {
                return "절전 요청 중";
            }

            return $"예상 절전까지 {Math.Ceiling(remaining)}초";
        }

        bool cpuBlocksAll = config.Idle.UseCpuActivity && !cpuUsageOk &&
                             (!config.Idle.UseInputActivity && !config.Idle.UseNetworkActivity);
        if (cpuBlocksAll)
        {
            return $"CPU 사용량 {cpuUsage:F1}%/{config.Idle.CpuUsagePercentageThreshold:F1}%";
        }

        bool networkBlocksAll = config.Idle.UseNetworkActivity && !networkUsageOk &&
                                (!config.Idle.UseInputActivity && !config.Idle.UseCpuActivity);
        if (networkBlocksAll)
        {
            return $"네트워크 {networkUsage:F0}/{config.Idle.NetworkKilobytesPerSecondThreshold:F0}KB/s";
        }

        var conditionRemaining = Math.Max(inputRemaining, Math.Max(cpuRemaining, networkRemaining));

        var totalRemaining = Math.Max(scheduleRemaining, conditionRemaining);
        totalRemaining = Math.Max(totalRemaining, cooldownRemainingSeconds);

        return $"예상 절전까지 {Math.Ceiling(Math.Max(0, totalRemaining))}초";
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

    private static double GetScheduleRemainingSeconds(ScheduleSettings schedule, DateTime nowLocal)
    {
        if (!schedule.Enabled)
        {
            return 0;
        }

        if (schedule.IsWithinWindow(nowLocal))
        {
            return 0;
        }

        var todayStart = nowLocal.Date + schedule.StartTime;

        if (schedule.StartTime <= schedule.EndTime)
        {
            if (nowLocal < todayStart)
            {
                return (todayStart - nowLocal).TotalSeconds;
            }

            return (todayStart.AddDays(1) - nowLocal).TotalSeconds;
        }

        if (nowLocal < todayStart)
        {
            return (todayStart - nowLocal).TotalSeconds;
        }

        return (todayStart.AddDays(1) - nowLocal).TotalSeconds;
    }

    private bool CanAttemptSleep(DateTime nowUtc) => GetCooldownRemainingSeconds(nowUtc) <= 0;

    private void TriggerSleep()
    {
        var attemptUtc = DateTime.UtcNow;
        var previousSuccess = _lastSleepSuccessUtc;

        if (_sleepService.TryEnterSleep(out var errorCode))
        {
            _lastSleepSuccessUtc = attemptUtc;
            var lastSuccessText = previousSuccess.HasValue
                ? previousSuccess.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "기록 없음";
            SleepTriggered?.Invoke(this, $"절전 명령 전송 (마지막 성공: {lastSuccessText})");
        }
        else
        {
            var lastSuccessText = _lastSleepSuccessUtc.HasValue
                ? _lastSleepSuccessUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "기록 없음";
            SleepTriggered?.Invoke(this, $"절전 모드 진입 실패 (오류 코드: {errorCode}, 마지막 성공: {lastSuccessText})");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void ApplySleepCooldownFromConfig()
    {
        var seconds = Math.Max(10, _config.SleepCooldownSeconds);
        _sleepCooldown = TimeSpan.FromSeconds(seconds);
        _config.SleepCooldownSeconds = seconds;
    }
}




