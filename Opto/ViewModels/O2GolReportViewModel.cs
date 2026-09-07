using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class O2GolReportViewModel : ViewModelBase
{
    private readonly O2GolReport _report;

    public string Title { get; }
    public string PeriodText { get; }
    public O2GolSummarySection ByBlocks { get; }
    public O2GolSummarySection ByWatches { get; }
    public O2GolOperatorGroup[] OperatorGroups { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand GoBackCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand ExportCsvCommand { get; }

    public O2GolReportViewModel() : this(
        new O2GolReport
        {
            From = DateTime.Today,
            To = DateTime.Today,
            ByBlocks = new O2GolSummarySection { Title = "", ColumnHeaders = [], Rows = [] },
            ByWatches = new O2GolSummarySection { Title = "", ColumnHeaders = [], Rows = [] },
            OperatorGroups = [],
        },
        () => { })
    {
    }

    public O2GolReportViewModel(O2GolReport report, Action goBack)
    {
        _report = report;
        Title = "Сводный расчёт по вахтам";
        PeriodText = $"Период: {report.From:dd.MM.yyyy} — {report.To:dd.MM.yyyy}";
        ByBlocks = report.ByBlocks;
        ByWatches = report.ByWatches;
        OperatorGroups = report.OperatorGroups.ToArray();
        GoBackCommand = new RelayCommand(goBack);
        PrintCommand = new RelayCommand(Print);
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync);
    }

    private async System.Threading.Tasks.Task ExportCsvAsync()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var dir = System.IO.Path.Combine(desktop, "Opto_Reports");
            System.IO.Directory.CreateDirectory(dir);

            var path = System.IO.Path.Combine(dir, $"o2gol_report_{_report.From:yyyyMMdd}_{_report.To:yyyyMMdd}.csv");
            var headers = new List<string> { "Секция", "Строка" };
            headers.AddRange(_report.ByBlocks.ColumnHeaders);
            headers.Add("Всего");

            var rows = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();

            foreach (var r in _report.ByBlocks.Rows)
            {
                var line = new System.Collections.Generic.List<string> { "По блокам", r.Label };
                line.AddRange(r.Values);
                line.Add(r.TotalText);
                rows.Add(line);
            }

            await ExportService.ExportToCsvAsync(path, headers, rows);
            StatusMessage = $"Экспортировано в Excel/CSV: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка экспорта: {ex.Message}";
        }
    }

    private void Print()
    {
        try
        {
            O2GolPrintService.Print(_report);
            StatusMessage = "Открыт диалог печати";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка печати: {ex.Message}";
        }
    }
}
