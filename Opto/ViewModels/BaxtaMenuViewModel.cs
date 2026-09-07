using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;

namespace Opto.ViewModels;

public partial class BaxtaMenuViewModel : ViewModelBase
{
    public ICommand SelectViewWatchScheduleCommand { get; }
    public ICommand SelectEditDataCommand { get; }
    public ICommand SelectPrintFormsCommand { get; }
    public ICommand SelectViewDatesCommand { get; }
    public ICommand SelectCopyDataCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<BaxtaMenuAction?>? Close { get; set; }

    public BaxtaMenuViewModel()
    {
        SelectViewWatchScheduleCommand = new RelayCommand(() => Close?.Invoke(BaxtaMenuAction.ViewWatchSchedule));
        SelectEditDataCommand = new RelayCommand(() => Close?.Invoke(BaxtaMenuAction.EditData));
        SelectPrintFormsCommand = new RelayCommand(() => Close?.Invoke(BaxtaMenuAction.PrintForms));
        SelectViewDatesCommand = new RelayCommand(() => Close?.Invoke(BaxtaMenuAction.ViewDates));
        SelectCopyDataCommand = new RelayCommand(() => Close?.Invoke(BaxtaMenuAction.CopyData));
        CancelCommand = new RelayCommand(() => Close?.Invoke(null));
    }
}
