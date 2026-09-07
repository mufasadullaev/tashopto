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
    public ICommand ExportCsvCommand { get; }
    public string BackButtonText { get; }

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

    public PerejegReportViewModel(
        PerejegReport report,
        Action goBack,
        string backButtonText = "← К вводу данных")
    {
        _report = report;
        BackButtonText = backButtonText;
        Title = report.IsCumulative
            ? "Нарастающий итог пережогов"
            : $"Суточные пережоги · {report.Date:dd.MM.yyyy}";
        Subtitle = report.IsCumulative
            ? $"Нарастающий итог · {report.PeriodFrom:dd.MM.yyyy} — {report.PeriodTo:dd.MM.yyyy}"
            : "Суточный расход топлива при отклонениях от тепловой схемы";
        Rows = new ObservableCollection<PerejegReportRow>(report.Rows);
        ReleaseText = report.ReleaseMlnKwh.ToString("0.######", OptoCulture.Current);
        TotalGkwhText = report.TotalGkwh.ToString("0.00", OptoCulture.Current);
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

            var path = System.IO.Path.Combine(dir, $"perejeg_report_{_report.Date:yyyyMMdd}.csv");
            var headers = new[] { "Отклонение", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "Станция" };
            var rows = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();

            foreach (var r in _report.Rows)
            {
                rows.Add(new System.Collections.Generic.List<string>
                {
                    r.Label, r.Col1, r.Col2, r.Col3, r.Col4, r.Col5, r.Col6,
                    r.Col7, r.Col8, r.Col9, r.Col10, r.Col11, r.Col12, r.StationText
                });
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
            PerejegPrintService.Print(_report);
            StatusMessage = "Открыт диалог печати";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка печати: {ex.Message}";
        }
    }
}
