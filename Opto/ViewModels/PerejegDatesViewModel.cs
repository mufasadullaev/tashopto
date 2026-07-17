using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Services;

namespace Opto.ViewModels;

public partial class PerejegDatesViewModel : ViewModelBase
{
    public ObservableCollection<PerejegCalculatedDateRow> Dates { get; } = [];

    public ICommand CloseCommand { get; }

    public PerejegDatesViewModel()
    {
        foreach (var date in PerejegStore.ListCalculatedDates())
        {
            Dates.Add(new PerejegCalculatedDateRow
            {
                Date = date,
                DateText = date.ToString("dd.MM.yyyy"),
            });
        }

        CloseCommand = new RelayCommand(() => Close?.Invoke());
    }

    public Action? Close { get; set; }
}

public sealed class PerejegCalculatedDateRow
{
    public required DateTime Date { get; init; }
    public required string DateText { get; init; }
}
