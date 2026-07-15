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

    public string Title { get; }
    public ObservableCollection<BaxtaShiftSection> Sections { get; }
    public string UrpText { get; }
    public string UrmText { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand GoBackCommand { get; }
    public ICommand PrintCommand { get; }

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

    public BaxtaReportViewModel(BaxtaReport report, Action goBack)
    {
        _report = report;
        Title = $"Результаты расчёта · {report.Date:dd.MM.yyyy}";
        Sections = new ObservableCollection<BaxtaShiftSection>(report.Sections);
        UrpText = report.Urp.ToString("0.00", OptoCulture.Current);
        UrmText = report.Urm.ToString("0.00", OptoCulture.Current);
        GoBackCommand = new RelayCommand(goBack);
        PrintCommand = new RelayCommand(Print);
    }

    private void Print()
    {
        try
        {
            BaxtaPrintService.Print(_report);
            StatusMessage = "Открыт диалог печати";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка печати: {ex.Message}";
        }
    }
}
