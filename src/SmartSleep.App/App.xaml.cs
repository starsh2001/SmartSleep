using System;
using System.Threading;
using System.Windows;
using SmartSleep.App.Models;
using SmartSleep.App.Services;
using SmartSleep.App.Utilities;
using SmartSleep.App.Views;

namespace SmartSleep.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Global\\SmartSleep.App";

    private ConfigurationService? _configurationService;
    private MonitoringService? _monitoringService;
    private TrayIconService? _trayIconService;
    private AutoStartService? _autoStartService;
    private SleepService? _sleepService;
    private AppConfig? _currentConfig;
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        bool createdNew;
        _singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out createdNew);
        if (!createdNew)
        {
            // Initialize with default English first for error messages
            LocalizationManager.Initialize();
            var title = LocalizationManager.GetString("App.DisplayName");
            var message = LocalizationManager.GetString("App.AlreadyRunning");
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        _configurationService = new ConfigurationService();
        _sleepService = new SleepService();
        _monitoringService = new MonitoringService(_sleepService);
        _autoStartService = new AutoStartService();

        var config = _configurationService.LoadAsync().GetAwaiter().GetResult();
        // Initialize localization with the user's preferred language
        LocalizationManager.Initialize();
        LocalizationManager.SetLanguage(config.Language);
        _currentConfig = config;

        if (config.StartWithWindows && !_autoStartService.IsEnabled())
        {
            _autoStartService.TrySetAutoStart(true, out _);
        }

        _monitoringService.UpdateConfiguration(config);
        _monitoringService.Start();

        _trayIconService = new TrayIconService(_monitoringService, CreateSettingsWindow, Dispatcher);
        _trayIconService.Initialize();
    }

    private SettingsWindow CreateSettingsWindow()
    {
        if (_configurationService == null || _monitoringService == null || _autoStartService == null || _currentConfig == null)
        {
            throw new InvalidOperationException(LocalizationManager.GetString("App.NotInitialized"));
        }

        var snapshot = _currentConfig.Clone();
        return new SettingsWindow(snapshot, _configurationService, _monitoringService, _autoStartService, OnSettingsSaved);
    }

    private void OnSettingsSaved(AppConfig updatedConfig)
    {
        LocalizationManager.SetLanguage(updatedConfig.Language);
        _currentConfig = updatedConfig.Clone();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        if (_monitoringService != null)
        {
            _monitoringService.StopAsync().GetAwaiter().GetResult();
            _monitoringService.Dispose();
        }

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;

        base.OnExit(e);
    }
}
