using System;
using System.Threading.Tasks;
using System.Windows;
using SmartSleep.App.Models;
using SmartSleep.App.Services;
using SmartSleep.App.ViewModels;

namespace SmartSleep.App.Views;

public partial class SettingsWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly MonitoringService _monitoringService;
    private readonly AutoStartService _autoStartService;
    private readonly Action<AppConfig> _onConfigSaved;
    private AppConfig _currentConfig;
    private bool _subscribed;

    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(AppConfig config,
                          ConfigurationService configurationService,
                          MonitoringService monitoringService,
                          AutoStartService autoStartService,
                          Action<AppConfig> onConfigSaved)
    {
        InitializeComponent();
        _currentConfig = config;
        _configurationService = configurationService;
        _monitoringService = monitoringService;
        _autoStartService = autoStartService;
        _onConfigSaved = onConfigSaved;

        ViewModel = SettingsViewModel.FromConfig(config);
        DataContext = ViewModel;

        _monitoringService.SnapshotAvailable += MonitoringServiceOnSnapshotAvailable;
        _subscribed = true;

        if (_monitoringService.LastSnapshot is { } snapshot)
        {
            ViewModel.UpdateLiveSnapshot(snapshot);
        }
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        await ApplyChangesAsync();
    }

    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (await ApplyChangesAsync())
        {
            Close();
        }
    }

    private async Task<bool> ApplyChangesAsync()
    {
        if (!ViewModel.TryValidate(out var validationError))
        {
            ViewModel.StatusMessage = validationError;
            return false;
        }

        try
        {
            ViewModel.StatusMessage = "적용 중...";
            var updatedConfig = ViewModel.ToConfig(_currentConfig);
            await _configurationService.SaveAsync(updatedConfig).ConfigureAwait(true);
            _monitoringService.UpdateConfiguration(updatedConfig);

            var autoStartResult = _autoStartService.TrySetAutoStart(updatedConfig.StartWithWindows, out var autoStartError);
            if (!autoStartResult)
            {
                ViewModel.StatusMessage = autoStartError ?? "자동 시작 설정에 실패했습니다.";
                return false;
            }

            _currentConfig = updatedConfig;
            _onConfigSaved(updatedConfig);

            if (_monitoringService.LastSnapshot is { } snapshot)
            {
                ViewModel.UpdateLiveSnapshot(snapshot);
            }

            ViewModel.StatusMessage = "적용되었습니다.";
            return true;
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = ex.Message;
            return false;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MonitoringServiceOnSnapshotAvailable(object? sender, MonitoringSnapshot snapshot)
    {
        Dispatcher.BeginInvoke(new Action(() => ViewModel.UpdateLiveSnapshot(snapshot)));
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_subscribed)
        {
            _monitoringService.SnapshotAvailable -= MonitoringServiceOnSnapshotAvailable;
            _subscribed = false;
        }

        base.OnClosed(e);
    }
}
