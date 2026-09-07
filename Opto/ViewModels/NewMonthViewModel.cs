using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Services;

namespace Opto.ViewModels;

public partial class NewMonthViewModel : ViewModelBase
{
    private readonly Action _goBack;

    [ObservableProperty]
    private bool _resetWatchSchedule = true;

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand GoBackCommand { get; }
    public ICommand ExecuteCommand { get; }

    public NewMonthViewModel(Action goBack)
    {
        _goBack = goBack;
        GoBackCommand = new RelayCommand(goBack);
        ExecuteCommand = new RelayCommand(Execute);
    }

    private void Execute()
    {
        try
        {
            NewMonthService.TryPrepareForNewMonth(ResetWatchSchedule);
            StatusMessage = ResetWatchSchedule
                ? "Переход выполнен: подготовлена выработка, график вахт сброшен."
                : "Переход выполнен: подготовлена выработка.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }
}
