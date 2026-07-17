using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class PerejegReportViewModel : ViewModelBase
{
    private readonly PerejegReport _report;

    public string Title { get; }
    public string Subtitle { get; }
    public ObservableCollection<PerejegReportRow> Rows { get; }
    public string ReleaseText { get; }
    public string TotalGkwhText { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand GoBackCommand { get; }
    public ICommand PrintCommand { get; }

    public PerejegReportViewModel() : this(
        new PerejegReport
        {
            Date = DateTime.Today,
            Mode = WyrabotkaMode.Calculation,
            Rows = [],
            ReleaseMlnKwh = 0,
            TotalGkwh = 0,
        },
        () => { })
    {
    }

    public PerejegReportViewModel(PerejegReport report, Action goBack)
    {
        _report = report;
        Title = $"Результаты расчёта · {report.Date:dd.MM.yyyy}";
        Subtitle = report.IsCumulative
            ? $"Нарастающий итог · {report.PeriodFrom:dd.MM.yyyy} — {report.PeriodTo:dd.MM.yyyy}"
            : "Суточный расход топлива при отклонениях от тепловой схемы";
        Rows = new ObservableCollection<PerejegReportRow>(report.Rows);
        ReleaseText = report.ReleaseMlnKwh.ToString("0.######", OptoCulture.Current);
        TotalGkwhText = report.TotalGkwh.ToString("0.00", OptoCulture.Current);
        GoBackCommand = new RelayCommand(goBack);
        PrintCommand = new RelayCommand(Print);
    }

    private void Print()
    {
        try
        {
            PerejegPrintService.Print(_report);
            StatusMessage = "Открыт диалог печати";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка печати: {ex.Message}";
        }
    }
}
