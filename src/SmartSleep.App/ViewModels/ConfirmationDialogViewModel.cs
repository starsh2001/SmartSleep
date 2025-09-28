using System.Windows;
using System.Windows.Threading;
using SmartSleep.App.Models;
using SmartSleep.App.Configuration;

namespace SmartSleep.App.ViewModels;

public class ConfirmationDialogViewModel : ViewModelBase
{
    private readonly DispatcherTimer _timer;
    private readonly int _totalSeconds;
    private int _remainingSeconds;
    private string _message = string.Empty;
    private string _countdownText = string.Empty;

    public event EventHandler? CountdownFinished;

    public ConfirmationDialogViewModel(PowerAction powerAction, int countdownSeconds)
    {
        _totalSeconds = countdownSeconds;
        _remainingSeconds = countdownSeconds;

        var actionText = powerAction switch
        {
            PowerAction.Sleep => System.Windows.Application.Current.FindResource("Confirmation_Action_Sleep") as string ?? "sleep mode",
            PowerAction.Shutdown => System.Windows.Application.Current.FindResource("Confirmation_Action_Shutdown") as string ?? "shutdown",
            _ => System.Windows.Application.Current.FindResource("Confirmation_Action_Default") as string ?? "power action"
        };

        var messageFormat = System.Windows.Application.Current.FindResource("Confirmation_Message") as string ?? "{0}s until {1}. Proceed?";
        Message = string.Format(messageFormat, countdownSeconds, actionText);
        UpdateCountdownText();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(DefaultValues.ConfirmationTimerIntervalMs / 1000)
        };
        _timer.Tick += OnTimerTick;
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string CountdownText
    {
        get => _countdownText;
        set => SetProperty(ref _countdownText, value);
    }

    public void StartCountdown()
    {
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        UpdateCountdownText();

        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            CountdownFinished?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateCountdownText()
    {
        var countdownFormat = System.Windows.Application.Current.FindResource("Confirmation_CountdownLabel") as string ?? "Remaining time: {0}s";
        CountdownText = string.Format(countdownFormat, _remainingSeconds);
    }

    public void StopCountdown()
    {
        _timer.Stop();
    }
}