using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;

namespace Opto.ViewModels;

public partial class BaxtaWatchScheduleViewModel : ViewModelBase
{
    public ObservableCollection<BaxtaWatchScheduleRow> Rows { get; }

    [ObservableProperty]
    private string? _hint;

    public ICommand CloseCommand { get; }

    public Action? Close { get; set; }

    public BaxtaWatchScheduleViewModel()
    {
        Rows = new ObservableCollection<BaxtaWatchScheduleRow>(BaxtaWatchStore.LoadSchedule());
        var next = Rows.FirstOrDefault(r => r.IsNext);
        Hint = next is null
            ? "Следующая строка для расчёта не отмечена."
            : $"При ближайшем расчёте будет использована строка {next.RowIndex}.";
        CloseCommand = new RelayCommand(() => Close?.Invoke());
    }
}
