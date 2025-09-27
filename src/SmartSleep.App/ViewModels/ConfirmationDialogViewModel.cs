using System.Windows.Threading;
using SmartSleep.App.Models;

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
            PowerAction.Sleep => "절전 모드",
            PowerAction.Shutdown => "시스템 종료",
            _ => "전원 동작"
        };

        Message = $"{countdownSeconds}초 뒤에 {actionText}가 실행됩니다.";
        UpdateCountdownText();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
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
        CountdownText = $"남은 시간: {_remainingSeconds}초";
    }

    public void StopCountdown()
    {
        _timer.Stop();
    }
}