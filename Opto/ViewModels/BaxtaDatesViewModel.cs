using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Opto.Services;

namespace Opto.ViewModels;

public partial class BaxtaDatesViewModel : ViewModelBase
{
    public ObservableCollection<BaxtaCalculatedDateRow> Dates { get; } = [];

    public ICommand CloseCommand { get; }

    public BaxtaDatesViewModel()
    {
        foreach (var date in BaxtaStore.ListCalculatedDates())
        {
            Dates.Add(new BaxtaCalculatedDateRow
            {
                Date = date,
                DateText = date.ToString("dd.MM.yyyy"),
            });
        }

        CloseCommand = new RelayCommand(() => Close?.Invoke());
    }

    public Action? Close { get; set; }
}

public sealed class BaxtaCalculatedDateRow
{
    public required DateTime Date { get; init; }
    public required string DateText { get; init; }
}
