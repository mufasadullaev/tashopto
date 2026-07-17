using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;

namespace Opto.ViewModels;

public partial class PerejegMenuViewModel : ViewModelBase
{
    public ICommand SelectDailyCalculationCommand { get; }
    public ICommand SelectPrintDailyCommand { get; }
    public ICommand SelectPrintCumulativeCommand { get; }
    public ICommand SelectViewDatesCommand { get; }
    public ICommand SelectPrintBlankCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<PerejegMenuAction?>? Close { get; set; }

    public PerejegMenuViewModel()
    {
        SelectDailyCalculationCommand = new RelayCommand(() => Close?.Invoke(PerejegMenuAction.DailyCalculation));
        SelectPrintDailyCommand = new RelayCommand(() => Close?.Invoke(PerejegMenuAction.PrintDaily));
        SelectPrintCumulativeCommand = new RelayCommand(() => Close?.Invoke(PerejegMenuAction.PrintCumulative));
        SelectViewDatesCommand = new RelayCommand(() => Close?.Invoke(PerejegMenuAction.ViewDates));
        SelectPrintBlankCommand = new RelayCommand(() => Close?.Invoke(PerejegMenuAction.PrintBlank));
        CancelCommand = new RelayCommand(() => Close?.Invoke(null));
    }
}
