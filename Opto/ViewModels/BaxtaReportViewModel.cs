using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class BaxtaReportViewModel : ViewModelBase
{
    private readonly BaxtaReport _report;
    private readonly int _printCopies;

    public string Title { get; }
    public string BackButtonText { get; }
    public string PrintButtonText { get; }
    public ObservableCollection<BaxtaShiftSection> Sections { get; }
    public string UrpText { get; }
    public string UrmText { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand GoBackCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand ExportCsvCommand { get; }

    public BaxtaReportViewModel() : this(
        new BaxtaReport
        {
            Date = DateTime.Today,
            Mode = WyrabotkaMode.Calculation,
            Sections = [],
            Urp = 0,
            Urm = 0,
        },
        () => { })
    {
    }

    public BaxtaReportViewModel(
        BaxtaReport report,
        Action goBack,
        string backButtonText = "← В подменю",
        int printCopies = 1)
    {
        _report = report;
        _printCopies = printCopies;
        BackButtonText = backButtonText;
        PrintButtonText = printCopies > 1 ? $"Печать · {printCopies} экз." : "Печать";
        Title = $"Результаты расчёта · {report.Date:dd.MM.yyyy}";
        Sections = new ObservableCollection<BaxtaShiftSection>(report.Sections);
        UrpText = report.Urp.ToString("0.00", OptoCulture.Current);
        UrmText = report.Urm.ToString("0.00", OptoCulture.Current);
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

            var path = System.IO.Path.Combine(dir, $"baxta_report_{_report.Date:yyyyMMdd}.csv");
            var headers = new[] { "Смена", "Строка", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "Всего" };
            var rows = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();

            foreach (var s in _report.Sections)
            {
                foreach (var r in s.Rows)
                {
                    rows.Add(new System.Collections.Generic.List<string>
                    {
                        s.Title, r.Label, r.Col1, r.Col2, r.Col3, r.Col4, r.Col5,
                        r.Col6, r.Col7, r.Col8, r.Col9, r.Col10, r.Col11, r.Col12, r.TotalText
                    });
                }
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
            BaxtaPrintService.Print(_report, _printCopies);
            StatusMessage = _printCopies > 1
                ? $"Открыт диалог печати · {_printCopies} экз."
                : "Открыт диалог печати";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка печати: {ex.Message}";
        }
    }
}
