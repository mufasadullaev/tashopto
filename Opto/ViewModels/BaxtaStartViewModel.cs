using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class BaxtaStartViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private DateTime? _selectedDate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private WyrabotkaMode? _selectedMode;

    [ObservableProperty]
    private string? _validationMessage;

    public IRelayCommand ContinueCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<BaxtaStartResult?>? Close { get; set; }

    public BaxtaStartViewModel()
    {
        ContinueCommand = new RelayCommand(Continue, CanContinue);
        CancelCommand = new RelayCommand(() => Close?.Invoke(null));
    }

    partial void OnSelectedModeChanged(WyrabotkaMode? value) => ValidationMessage = null;
    partial void OnSelectedDateChanged(DateTime? value) => ValidationMessage = null;

    public void SelectCalculation() => SelectedMode = WyrabotkaMode.Calculation;
    public void SelectRecalculation() => SelectedMode = WyrabotkaMode.Recalculation;

    private bool CanContinue() => SelectedDate is not null && SelectedMode is not null;

    private void Continue()
    {
        if (SelectedDate is null || SelectedMode is null)
        {
            ValidationMessage = "Укажите дату и выберите режим: Расчёт или Перерасчёт.";
            return;
        }

        var watchError = BaxtaWatchStore.TryValidate(SelectedDate.Value.Date, SelectedMode.Value);
        if (watchError is not null)
        {
            ValidationMessage = watchError;
            return;
        }

        var earliest = BaxtaStore.GetEarliestDay();
        if (earliest is not null && SelectedDate.Value.Date < earliest.Value.Date)
        {
            ValidationMessage = "Исходные данные за этот период отсутствуют.";
            return;
        }

        if (SelectedMode == WyrabotkaMode.Recalculation && !BaxtaStore.HasDay(SelectedDate.Value.Date))
        {
            ValidationMessage = "За эту дату нет сохранённых данных. Выберите «Расчёт».";
            return;
        }

        Close?.Invoke(new BaxtaStartResult
        {
            Date = SelectedDate.Value.Date,
            Mode = SelectedMode.Value,
        });
    }
}
