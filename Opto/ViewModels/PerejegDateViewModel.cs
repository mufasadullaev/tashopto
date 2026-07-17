using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Opto.ViewModels;

public partial class PerejegDateViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private DateTime? _selectedDate;

    [ObservableProperty]
    private string? _validationMessage;

    public string Title { get; }
    public string Prompt { get; }

    public IRelayCommand ContinueCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<DateTime?>? Close { get; set; }

    public PerejegDateViewModel(string title, string prompt)
    {
        Title = title;
        Prompt = prompt;
        ContinueCommand = new RelayCommand(Continue, () => SelectedDate is not null);
        CancelCommand = new RelayCommand(() => Close?.Invoke(null));
    }

    partial void OnSelectedDateChanged(DateTime? value) => ValidationMessage = null;

    private void Continue()
    {
        if (SelectedDate is null)
        {
            ValidationMessage = "Укажите дату.";
            return;
        }

        Close?.Invoke(SelectedDate.Value.Date);
    }
}
