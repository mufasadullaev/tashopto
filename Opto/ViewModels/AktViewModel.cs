using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class AktViewModel : ViewModelBase
{
    private readonly Action _goBack;
    private readonly Action<ViewModelBase> _navigate;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private decimal _reserveExciterKwh;

    [ObservableProperty]
    private decimal _lossesKwh;

    [ObservableProperty]
    private decimal _plantFacilitiesKwh;

    [ObservableProperty]
    private decimal _preventoriumKwh;

    [ObservableProperty]
    private AktReport? _report;

    public ICommand CalculateCommand { get; }
    public ICommand BackCommand { get; }

    public AktViewModel(Action goBack, Action<ViewModelBase> navigate)
    {
        _goBack = goBack;
        _navigate = navigate;

        CalculateCommand = new RelayCommand(Calculate);
        BackCommand = new RelayCommand(goBack);

        LoadSaved();
    }

    private void LoadSaved()
    {
        var saved = AktStore.TryLoad(Date);
        if (saved is not null)
        {
            ReserveExciterKwh = saved.ReserveExciterKwh;
            LossesKwh = saved.LossesKwh;
            PlantFacilitiesKwh = saved.PlantFacilitiesKwh;
            PreventoriumKwh = saved.PreventoriumKwh;
        }

        Calculate();
    }

    private void Calculate()
    {
        var input = new AktInputSnapshot
        {
            Date = Date,
            ReserveExciterKwh = ReserveExciterKwh,
            LossesKwh = LossesKwh,
            PlantFacilitiesKwh = PlantFacilitiesKwh,
            PreventoriumKwh = PreventoriumKwh,
        };

        AktStore.Save(Date, input);

        var monthWyr = WyrabotkaStore.TryLoadResult(Date);
        Report = AktCalculator.Calculate(Date, monthWyr, input);
    }
}
