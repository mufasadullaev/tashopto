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
    private readonly Action _goBackToSubmenu;
    private readonly int[] _watchByShift;

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
        Action goBackToSubmenu,
        Action<ViewModelBase> navigate)
    {
        Date = start.Date;
        Mode = start.Mode;
        _navigate = navigate;
        _goBackToSubmenu = goBackToSubmenu;
        _watchByShift = BaxtaWatchStore.ResolveWatches(Date, Mode);
        Title =
            $"Ежедневный расчёт по вахтам · {Date:dd.MM.yyyy} · {(Mode == WyrabotkaMode.Calculation ? "Расчёт" : "Перерасчёт")}";

        SeedDefaults();

        var saved = BaxtaStore.TryLoad(Date);
        var templateSource = saved is null ? BaxtaStore.TryLoadTemplateSnapshot(Date) : null;
        (saved ?? templateSource)?.ApplyTo(Meters, Coeffs, Thermo, Plant);
        if (saved is null && templateSource is not null)
        {
            var templateDate = BaxtaStore.GetEarliestDay();
            StatusMessage = templateDate is null
                ? null
                : $"Шаблон скопирован с {templateDate.Value:dd.MM.yyyy}";
        }
        EnsureKaratCoefficients();
        SyncPrisesFromPlant();

        CancelCommand = new RelayCommand(goBackToSubmenu);
        SaveCommand = new RelayCommand(Save);
        CalculateCommand = new RelayCommand(Calculate);
    }

    // Коэффициенты из karat BAXTA4.DBF (KL1, KL2, KF1, KF2, KF3, KF4)
    private static readonly decimal[][] KaratBlockCoeffs =
    [
        [0.333m, 223m, 4.06m, 0.017m, 4.45m, 0.021m],
        [0.360m, 224m, 4.06m, 0.017m, 5.55m, 0.018m],
        [0.360m, 224m, 4.06m, 0.017m, 5.55m, 0.018m],
        [0.360m, 224m, 4.06m, 0.017m, 4.45m, 0.021m],
        [0.367m, 225m, 4.06m, 0.017m, 4.45m, 0.021m],
        [0.347m, 222m, 4.08m, 0.022m, 4.49m, 0.024m],
        [0.360m, 224m, 4.08m, 0.022m, 4.49m, 0.024m],
        [0.333m, 220m, 4.08m, 0.022m, 4.49m, 0.024m],
        [0.327m, 219m, 4.08m, 0.022m, 4.49m, 0.024m],
        [0.373m, 226m, 4.08m, 0.022m, 4.49m, 0.024m],
        [0.387m, 228m, 4.08m, 0.022m, 4.49m, 0.024m],
        [0.367m, 225m, 4.08m, 0.022m, 4.49m, 0.024m],
    ];

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

            var c = KaratBlockCoeffs[i - 1];
            Coeffs.Add(new BaxtaBlockCoeffsRow
            {
                Number = i,
                Kl1 = c[0],
                Kl2 = c[1],
                Kf1 = c[2],
                Kf2 = c[3],
                Kf3 = c[4],
                Kf4 = c[5],
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

    private void EnsureKaratCoefficients()
    {
        foreach (var row in Coeffs)
        {
            if (row.Number is < 1 or > 12)
                continue;

            // Старые сохранения: kf=0 или заглушки kl1=1.7/kl2=5.3
            var looksBroken = (row.Kf1 == 0m && row.Kf2 == 0m)
                || row.Kl1 == 1.7m
                || row.Kl2 == 5.3m;
            if (!looksBroken)
                continue;

            var c = KaratBlockCoeffs[row.Number - 1];
            row.Kl1 = c[0];
            row.Kl2 = c[1];
            row.Kf1 = c[2];
            row.Kf2 = c[3];
            row.Kf3 = c[4];
            row.Kf4 = c[5];
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

        var (report, details) = BaxtaCalculator.CalculateWithDetails(
            Date, Mode, Meters, Thermo, Plant, Coeffs, _watchByShift);
        BaxtaStore.SaveCalculationResult(Date, report, details);
        if (Mode == WyrabotkaMode.Calculation)
            BaxtaWatchStore.AdvanceAfterCalculation(Date);
        _navigate(new BaxtaReportViewModel(
            report,
            _goBackToSubmenu,
            backButtonText: "← В подменю"));
    }
}
