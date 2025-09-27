using System.Windows;
using SmartSleep.App.ViewModels;

namespace SmartSleep.App.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(ConfirmationDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CountdownFinished += OnCountdownFinished;
        viewModel.StartCountdown();
    }

    private void OnCountdownFinished(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Execute_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}