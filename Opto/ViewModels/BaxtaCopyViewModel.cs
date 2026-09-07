using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Services;

namespace Opto.ViewModels;

public partial class BaxtaCopyViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    private DateTime? _sourceDate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    private DateTime? _targetDate;

    [ObservableProperty]
    private string? _validationMessage;

    public IRelayCommand CopyCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<bool>? Close { get; set; }

    public BaxtaCopyViewModel()
    {
        CopyCommand = new RelayCommand(Copy, () => SourceDate is not null && TargetDate is not null);
        CancelCommand = new RelayCommand(() => Close?.Invoke(false));
    }

    partial void OnSourceDateChanged(DateTime? value) => ValidationMessage = null;
    partial void OnTargetDateChanged(DateTime? value) => ValidationMessage = null;

    private void Copy()
    {
        if (SourceDate is null || TargetDate is null)
        {
            ValidationMessage = "Укажите обе даты.";
            return;
        }

        var error = BaxtaStore.TryCopyDay(SourceDate.Value.Date, TargetDate.Value.Date);
        if (error is not null)
        {
            ValidationMessage = error;
            return;
        }

        Close?.Invoke(true);
    }
}
