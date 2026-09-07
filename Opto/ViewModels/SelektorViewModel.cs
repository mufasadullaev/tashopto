using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class SelektorViewModel : ViewModelBase
{
    private readonly Action _goBack;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private SelektorReport? _report;

    public ICommand CalculateCommand { get; }
    public ICommand BackCommand { get; }

    public SelektorViewModel(Action goBack)
    {
        _goBack = goBack;

        CalculateCommand = new RelayCommand(Calculate);
        BackCommand = new RelayCommand(goBack);

        Calculate();
    }

    private void Calculate()
    {
        var wyr = WyrabotkaStore.TryLoadResult(Date);
        Report = SelektorCalculator.Calculate(Date, wyr);
    }
}
