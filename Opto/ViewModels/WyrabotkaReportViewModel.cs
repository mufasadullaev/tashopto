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
    public ICommand ExportCsvCommand { get; }

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
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync);
    }

    private async System.Threading.Tasks.Task ExportCsvAsync()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var dir = System.IO.Path.Combine(desktop, "Opto_Reports");
            System.IO.Directory.CreateDirectory(dir);

            var path = System.IO.Path.Combine(dir, $"wyrabotka_report_{_report.Date:yyyyMMdd}.csv");
            var headers = new[] { "Блок/Трансформатор", "Часы", "Выработка (тыс. кВт·ч)", "Выработка (МВт)", "СН (тыс. кВт·ч)", "СН (МВт)", "% СН" };
            var rows = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();

            foreach (var b in _report.BlockRows)
            {
                rows.Add(new System.Collections.Generic.List<string>
                {
                    $"Блок {b.Label}", b.Hours?.ToString() ?? "", b.GenerationThousandKwh.ToString(), b.GenerationMw.ToString(), b.OwnNeedsThousandKwh.ToString(), b.OwnNeedsMw.ToString(), b.Percent.ToString()
                });
            }

            foreach (var tr in _report.TransformerRows)
            {
                rows.Add(new System.Collections.Generic.List<string>
                {
                    tr.Label, "", "", "", tr.OwnNeedsThousandKwh.ToString(), "", ""
                });
            }

            rows.Add(new System.Collections.Generic.List<string>
            {
                "ИТОГО ЗА СУТКИ", _report.DayTotal.Hours?.ToString() ?? "", _report.DayTotal.GenerationThousandKwh.ToString(), _report.DayTotal.GenerationMw.ToString(), _report.DayTotal.OwnNeedsThousandKwh.ToString(), _report.DayTotal.OwnNeedsMw.ToString(), _report.DayTotal.Percent.ToString()
            });

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
            WyrabotkaPrintService.Print(_report);
            StatusMessage = "Открыт диалог печати";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка печати: {ex.Message}";
        }
    }
}
