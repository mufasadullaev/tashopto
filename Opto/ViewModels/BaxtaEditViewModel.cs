using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class BaxtaEditViewModel : ViewModelBase
{
    private static readonly decimal[] GenerationCoefficients =
        [2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880];

    private static readonly decimal[] OwnNeedsCoefficients =
        [7200, 7200, 7200, 7200, 7200, 7200, 5400, 7200, 7200, 7200, 7200, 7200];

    private static readonly string[] ShiftLabels = ["0–8 ч", "8–16 ч", "16–24 ч"];

    private readonly Action<ViewModelBase> _navigate;

    public DateTime Date { get; }
    public WyrabotkaMode Mode { get; }
    public string Title { get; }

    public ObservableCollection<BaxtaMeterBlockRow> Meters { get; } = [];
    public ObservableCollection<BaxtaBlockCoeffsRow> Coeffs { get; } = [];
    public ObservableCollection<BaxtaThermoRow> Thermo { get; } = [];
    public ObservableCollection<BaxtaPrisRow> Prises { get; } = [];
    public BaxtaPlantParamsRow Plant { get; } = new();

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand CancelCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CalculateCommand { get; }

    public BaxtaEditViewModel() : this(
        new BaxtaStartResult
        {
            Date = DateTime.Today,
            Mode = WyrabotkaMode.Calculation,
        },
        () => { },
        _ => { })
    {
    }

    public BaxtaEditViewModel(
        BaxtaStartResult start,
        Action goBack,
        Action<ViewModelBase> navigate)
    {
        Date = start.Date;
        Mode = start.Mode;
        _navigate = navigate;
        Title =
            $"Ежедневный расчёт по вахтам · {Date:dd.MM.yyyy} · {(Mode == WyrabotkaMode.Calculation ? "Расчёт" : "Перерасчёт")}";

        SeedDefaults();

        var saved = BaxtaStore.TryLoad(Date);
        saved?.ApplyTo(Meters, Coeffs, Thermo, Plant);
        SyncPrisesFromPlant();

        CancelCommand = new RelayCommand(goBack);
        SaveCommand = new RelayCommand(Save);
        CalculateCommand = new RelayCommand(Calculate);
    }

    private void SeedDefaults()
    {
        for (var i = 1; i <= 12; i++)
        {
            Meters.Add(new BaxtaMeterBlockRow
            {
                Number = i,
                GenerationCoefficient = GenerationCoefficients[i - 1],
                OwnNeedsCoefficient = OwnNeedsCoefficients[i - 1],
            });

            Coeffs.Add(new BaxtaBlockCoeffsRow
            {
                Number = i,
                Kl1 = 1.7m,
                Kl2 = 5.3m,
            });

            Prises.Add(new BaxtaPrisRow { BlockNumber = i, Value = Plant.Prises[i - 1] });

            for (var shift = 1; shift <= 3; shift++)
            {
                Thermo.Add(new BaxtaThermoRow
                {
                    BlockNumber = i,
                    ShiftIndex = shift,
                    ShiftLabel = ShiftLabels[shift - 1],
                });
            }
        }
    }

    private void SyncPrisesFromPlant()
    {
        for (var i = 0; i < Prises.Count; i++)
            Prises[i].Value = Plant.Prises[i];
    }

    private void SyncPrisesToPlant()
    {
        for (var i = 0; i < Prises.Count; i++)
            Plant.Prises[i] = Prises[i].Value;
    }

    private void Save()
    {
        SyncPrisesToPlant();
        if (!BaxtaInputValidator.Validate(Meters, Plant, Thermo, out var error))
        {
            StatusMessage = error;
            return;
        }

        var snapshot = BaxtaEditSnapshot.FromRows(Meters, Coeffs, Thermo, Plant);
        BaxtaStore.Save(Date, Mode, snapshot);
        StatusMessage = $"Сохранено · {DateTime.Now:HH:mm:ss}";
    }

    private void Calculate()
    {
        SyncPrisesToPlant();
        if (!BaxtaInputValidator.Validate(Meters, Plant, Thermo, out var error))
        {
            StatusMessage = error;
            return;
        }

        var snapshot = BaxtaEditSnapshot.FromRows(Meters, Coeffs, Thermo, Plant);
        BaxtaStore.Save(Date, Mode, snapshot);

        var report = BaxtaCalculator.Calculate(Date, Mode, Meters, Thermo, Plant, Coeffs);
        BaxtaStore.SaveResult(Date, report);
        _navigate(new BaxtaReportViewModel(report, () => _navigate(this)));
    }
}
