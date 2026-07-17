using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class PerejegEditViewModel : ViewModelBase
{
    private readonly Action<ViewModelBase> _navigate;

    public DateTime Date { get; }
    public WyrabotkaMode Mode { get; }
    public string Title { get; }

    public ObservableCollection<PerejegInputRow> Rows { get; } = [];

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand CancelCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CalculateCommand { get; }

    public PerejegEditViewModel() : this(
        new PerejegStartResult
        {
            Date = DateTime.Today,
            Mode = WyrabotkaMode.Calculation,
        },
        () => { },
        _ => { })
    {
    }

    public PerejegEditViewModel(
        PerejegStartResult start,
        Action goBack,
        Action<ViewModelBase> navigate)
    {
        Date = start.Date;
        Mode = start.Mode;
        _navigate = navigate;
        Title =
            $"Пережоги топлива · {Date:dd.MM.yyyy} · {(Mode == WyrabotkaMode.Calculation ? "Расчёт" : "Перерасчёт")}";

        foreach (var row in PerejegInputFactory.CreateEmptyRows())
            Rows.Add(row);

        var saved = PerejegStore.TryLoad(Date);
        if (saved is not null)
        {
            saved.ApplyTo(Rows);
        }
        else
        {
            var release = PerejegStore.TryGetReleaseMlnKwh(Date);
            if (release is not null)
                Rows[15].Station = release.Value;
        }

        CancelCommand = new RelayCommand(goBack);
        SaveCommand = new RelayCommand(Save);
        CalculateCommand = new RelayCommand(Calculate);
    }

    private void Save()
    {
        if (!PerejegInputValidator.Validate(Rows, out var error))
        {
            StatusMessage = error;
            return;
        }

        var snapshot = PerejegEditSnapshot.FromRows(Rows);
        PerejegStore.Save(Date, Mode, snapshot);
        StatusMessage = $"Сохранено · {DateTime.Now:HH:mm:ss}";
    }

    private void Calculate()
    {
        if (!PerejegInputValidator.Validate(Rows, out var error))
        {
            StatusMessage = error;
            return;
        }

        var snapshot = PerejegEditSnapshot.FromRows(Rows);
        PerejegStore.Save(Date, Mode, snapshot);

        var report = PerejegCalculator.Calculate(Date, Mode, Rows);
        PerejegStore.SaveResult(Date, report);
        _navigate(new PerejegReportViewModel(report, () => _navigate(this)));
    }
}
