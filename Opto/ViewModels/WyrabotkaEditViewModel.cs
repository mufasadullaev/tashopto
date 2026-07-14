using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class WyrabotkaEditViewModel : ViewModelBase
{
    private static readonly decimal[] GenerationCoefficients =
        [2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880, 2880];

    private static readonly decimal[] OwnNeedsCoefficients =
        [7200, 7200, 7200, 7200, 7200, 7200, 5400, 7200, 7200, 7200, 7200, 7200];

    private static readonly (string Name, decimal Coefficient)[] TransformerDefaults =
    [
        ("ТС-20", 2400),
        ("ТС-30", 2400),
        ("3ТР-А", 2400),
        ("3ТР-В", 2400),
    ];

    private readonly Action<ViewModelBase> _navigate;

    public DateTime Date { get; }
    public WyrabotkaMode Mode { get; }

    public string Title { get; }

    public ObservableCollection<BlockMeterRow> Blocks { get; } = [];
    public ObservableCollection<TransformerMeterRow> Transformers { get; } = [];

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand CancelCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CalculateCommand { get; }

    public WyrabotkaEditViewModel() : this(
        new WyrabotkaStartResult
        {
            Date = DateTime.Today,
            Mode = WyrabotkaMode.Calculation,
        },
        () => { },
        _ => { })
    {
    }

    public WyrabotkaEditViewModel(
        WyrabotkaStartResult start,
        Action goBack,
        Action<ViewModelBase> navigate)
    {
        Date = start.Date;
        Mode = start.Mode;
        _navigate = navigate;
        Title =
            $"Выработка электроэнергии · {Date:dd.MM.yyyy} · {(Mode == WyrabotkaMode.Calculation ? "Расчёт" : "Перерасчёт")}";

        for (var i = 1; i <= 12; i++)
        {
            Blocks.Add(new BlockMeterRow
            {
                Number = i,
                GenerationCoefficient = GenerationCoefficients[i - 1],
                OwnNeedsCoefficient = OwnNeedsCoefficients[i - 1],
            });
        }

        foreach (var (name, coefficient) in TransformerDefaults)
        {
            Transformers.Add(new TransformerMeterRow
            {
                Name = name,
                Coefficient = coefficient,
            });
        }

        var draft = WyrabotkaDraftStore.TryLoad(Date);
        draft?.ApplyTo(Blocks, Transformers);

        CancelCommand = new RelayCommand(goBack);
        SaveCommand = new RelayCommand(Save);
        CalculateCommand = new RelayCommand(Calculate);
    }

    private void Save()
    {
        if (!WyrabotkaInputValidator.Validate(Blocks, Transformers, out var error))
        {
            StatusMessage = error;
            return;
        }

        var snapshot = WyrabotkaEditSnapshot.FromRows(Blocks, Transformers);
        WyrabotkaDraftStore.Save(Date, Mode, snapshot);
        StatusMessage = $"Сохранено · {DateTime.Now:HH:mm:ss}";
    }

    private void Calculate()
    {
        if (!WyrabotkaInputValidator.Validate(Blocks, Transformers, out var error))
        {
            StatusMessage = error;
            return;
        }

        var snapshot = WyrabotkaEditSnapshot.FromRows(Blocks, Transformers);
        WyrabotkaDraftStore.Save(Date, Mode, snapshot);

        var report = WyrabotkaCalculator.Calculate(Date, Mode, Blocks, Transformers);
        _navigate(new WyrabotkaReportViewModel(report, () => _navigate(this)));
    }
}
