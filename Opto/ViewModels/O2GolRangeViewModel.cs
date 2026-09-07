using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Opto.ViewModels;

public partial class O2GolRangeViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private DateTime? _fromDate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private DateTime? _toDate;

    [ObservableProperty]
    private string? _validationMessage;

    public IRelayCommand ContinueCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<(DateTime From, DateTime To)?>? Close { get; set; }

    public O2GolRangeViewModel()
    {
        ContinueCommand = new RelayCommand(Continue, CanContinue);
        CancelCommand = new RelayCommand(() => Close?.Invoke(null));
    }

    partial void OnFromDateChanged(DateTime? value) => ValidationMessage = null;
    partial void OnToDateChanged(DateTime? value) => ValidationMessage = null;

    private bool CanContinue() => FromDate is not null && ToDate is not null;

    private void Continue()
    {
        if (FromDate is null || ToDate is null)
        {
            ValidationMessage = "Укажите начальную и конечную дату.";
            return;
        }

        if (FromDate.Value.Date > ToDate.Value.Date)
        {
            ValidationMessage = "Начальная дата не может быть позже конечной.";
            return;
        }

        Close?.Invoke((FromDate.Value.Date, ToDate.Value.Date));
    }
}
