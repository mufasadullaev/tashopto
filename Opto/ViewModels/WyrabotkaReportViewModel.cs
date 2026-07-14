using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class WyrabotkaReportViewModel : ViewModelBase
{
    private readonly WyrabotkaReport _report;

    public string Title { get; }

    public ObservableCollection<WyrabotkaReportRow> BlockRows { get; }
    public ObservableCollection<WyrabotkaReportRow> TransformerRows { get; }

    public WyrabotkaReportRow DayTotal { get; }
    public WyrabotkaReportRow MonthTotal { get; }

    public string DayReleaseText { get; }
    public string MonthReleaseText { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand GoBackCommand { get; }
    public ICommand PrintCommand { get; }

    public WyrabotkaReportViewModel(WyrabotkaReport report, Action goBack)
    {
        _report = report;
        Title = $"Результаты расчёта · {report.Date:dd.MM.yyyy}";

        BlockRows = new ObservableCollection<WyrabotkaReportRow>(report.BlockRows);
        TransformerRows = new ObservableCollection<WyrabotkaReportRow>(report.TransformerRows);
        DayTotal = report.DayTotal;
        MonthTotal = report.MonthTotal;
        DayReleaseText = report.DayRelease.ToString("0.00", OptoCulture.Current);
        MonthReleaseText = report.MonthRelease.ToString("0.00", OptoCulture.Current);
        GoBackCommand = new RelayCommand(goBack);
        PrintCommand = new RelayCommand(Print);
    }

    private void Print()
    {
        try
        {
            WyrabotkaPrintService.Print(_report);
            StatusMessage = "Открыт диалог печати";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка печати: {ex.Message}";
        }
    }
}
