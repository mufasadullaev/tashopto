using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
using Opto.Services;
using Opto.Views;

namespace Opto.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly MainMenuViewModel _menuPage;
    private TaskCompletionSource? _baxtaFlowCompletion;

    [ObservableProperty]
    private ViewModelBase _currentPage = null!;

    public ICommand ExitCommand { get; }

    public MainViewModel()
    {
        ExitCommand = new RelayCommand(Exit);
        var open = new AsyncRelayCommand<MenuAction>(OpenModuleAsync);
        _menuPage = new MainMenuViewModel(open, ExitCommand);
        CurrentPage = _menuPage;
    }

    public void NavigateTo(ViewModelBase page) => CurrentPage = page;

    public void GoToMenu() => CurrentPage = _menuPage;

    private async Task OpenModuleAsync(MenuAction? action)
    {
        if (action is null)
            return;

        if (action.Id == "wyrabotka")
        {
            await OpenWyrabotkaAsync();
            return;
        }

        if (action.Id == "baxta")
        {
            await OpenBaxtaAsync();
            return;
        }

        if (action.Id == "perejeg")
        {
            await OpenPerejegAsync();
            return;
        }

        if (action.Id == "o2gol")
        {
            await OpenO2GolAsync();
            return;
        }

        if (action.Id == "akt")
        {
            NavigateTo(new AktViewModel(GoToMenu, NavigateTo));
            return;
        }

        if (action.Id == "selektor")
        {
            NavigateTo(new SelektorViewModel(GoToMenu));
            return;
        }

        if (action.Id == "backup")
        {
            NavigateTo(new BackupViewModel(GoToMenu));
            return;
        }

        if (action.Id == "manual")
        {
            NavigateTo(new ManualViewModel(GoToMenu));
            return;
        }

        if (action.Id == "service")
        {
            NavigateTo(new ServiceViewModel(GoToMenu));
            return;
        }

        if (action.Id == "help")
        {
            NavigateTo(new HelpViewModel(GoToMenu));
            return;
        }

        NavigateTo(new PlaceholderViewModel(action.Title, GoToMenu));
    }

    private async Task OpenWyrabotkaAsync()
    {
        var owner = GetMainWindow();
        if (owner is null)
            return;

        var startVm = new WyrabotkaStartViewModel();
        var dialog = new WyrabotkaStartWindow(startVm);
        var result = await dialog.ShowDialog<WyrabotkaStartResult?>(owner);

        if (result is null)
            return;

        NavigateTo(new WyrabotkaEditViewModel(result, GoToMenu, NavigateTo));
    }

    private async Task OpenBaxtaAsync()
    {
        var owner = GetMainWindow();
        if (owner is null)
            return;

        while (true)
        {
            var menuVm = new BaxtaMenuViewModel();
            var menu = new BaxtaMenuWindow(menuVm);
            var action = await menu.ShowDialog<BaxtaMenuAction?>(owner);
            if (action is null)
                return;

            switch (action.Value)
            {
                case BaxtaMenuAction.ViewWatchSchedule:
                {
                    var scheduleVm = new BaxtaWatchScheduleViewModel();
                    var scheduleWindow = new BaxtaWatchScheduleWindow(scheduleVm);
                    await scheduleWindow.ShowDialog(owner);
                    break;
                }

                case BaxtaMenuAction.EditData:
                    if (await TryOpenBaxtaEditAsync(owner))
                        await WaitForBaxtaFlowAsync();
                    break;

                case BaxtaMenuAction.PrintForms:
                    if (await TryShowBaxtaPrintReportAsync(owner))
                        await WaitForBaxtaFlowAsync();
                    break;

                case BaxtaMenuAction.ViewDates:
                    await ShowBaxtaDatesAsync(owner);
                    break;

                case BaxtaMenuAction.CopyData:
                    await CopyBaxtaDataAsync(owner);
                    break;
            }
        }
    }

    private void ResumeBaxtaSubmenu()
    {
        GoToMenu();
        _baxtaFlowCompletion?.TrySetResult();
    }

    private async Task WaitForBaxtaFlowAsync()
    {
        _baxtaFlowCompletion = new TaskCompletionSource();
        await _baxtaFlowCompletion.Task;
        _baxtaFlowCompletion = null;
    }

    private async Task<bool> TryOpenBaxtaEditAsync(Window owner)
    {
        var startVm = new BaxtaStartViewModel();
        var dialog = new BaxtaStartWindow(startVm);
        var result = await dialog.ShowDialog<BaxtaStartResult?>(owner);
        if (result is null)
            return false;

        if (result.Mode == WyrabotkaMode.Calculation && result.Date.Day == 1)
        {
            var changeMonth = await ShowConfirmAsync(
                owner,
                "Менять месяц?",
                "Первое число месяца. Сбросить график вахт для нового месяца?");
            if (changeMonth == true)
                BaxtaWatchStore.ResetForNewMonth();
        }

        NavigateTo(new BaxtaEditViewModel(result, ResumeBaxtaSubmenu, NavigateTo));
        return true;
    }

    private async Task<bool> TryShowBaxtaPrintReportAsync(Window owner)
    {
        var dateVm = new PerejegDateViewModel(
            "Печать выводных форм",
            "Укажите дату расчёта");
        var dateDialog = new PerejegDateWindow(dateVm);
        var date = await dateDialog.ShowDialog<DateTime?>(owner);
        if (date is null)
            return false;

        var report = BaxtaStore.TryBuildReport(date.Value);
        if (report is null)
        {
            await ShowMessageAsync(owner, "Данные за это число отсутствуют.");
            return false;
        }

        NavigateTo(new BaxtaReportViewModel(
            report,
            ResumeBaxtaSubmenu,
            backButtonText: "← В подменю",
            printCopies: 5));
        return true;
    }

    private static async Task ShowBaxtaDatesAsync(Window owner)
    {
        var datesVm = new BaxtaDatesViewModel();
        var datesWindow = new BaxtaDatesWindow(datesVm);
        await datesWindow.ShowDialog(owner);
    }

    private static async Task CopyBaxtaDataAsync(Window owner)
    {
        var copyVm = new BaxtaCopyViewModel();
        var copyWindow = new BaxtaCopyWindow(copyVm);
        var copied = await copyWindow.ShowDialog<bool>(owner);
        if (copied)
            await ShowMessageAsync(owner, "Данные скопированы.");
    }

    private async Task OpenO2GolAsync()
    {
        var owner = GetMainWindow();
        if (owner is null)
            return;

        var rangeVm = new O2GolRangeViewModel();
        var rangeDialog = new O2GolRangeWindow(rangeVm);
        var range = await rangeDialog.ShowDialog<(DateTime From, DateTime To)?>(owner);
        if (range is null)
            return;

        var report = O2GolStore.TryBuildReport(range.Value.From, range.Value.To);
        if (report is null)
        {
            await ShowMessageAsync(owner, "Данные за этот период отсутствуют. Нужны рассчитанные суточные отчёты по вахтам.");
            return;
        }

        NavigateTo(new O2GolReportViewModel(report, GoToMenu));
    }

    private async Task OpenPerejegAsync()
    {
        var owner = GetMainWindow();
        if (owner is null)
            return;

        while (true)
        {
            var menuVm = new PerejegMenuViewModel();
            var menu = new PerejegMenuWindow(menuVm);
            var action = await menu.ShowDialog<PerejegMenuAction?>(owner);
            if (action is null)
                return;

            switch (action.Value)
            {
                case PerejegMenuAction.DailyCalculation:
                    await OpenPerejegDailyCalculationAsync(owner);
                    return;

                case PerejegMenuAction.PrintDaily:
                    if (await TryShowPerejegDailyReportAsync(owner))
                        return;
                    break;

                case PerejegMenuAction.PrintCumulative:
                    if (await TryShowPerejegCumulativeReportAsync(owner))
                        return;
                    break;

                case PerejegMenuAction.ViewDates:
                    await ShowPerejegDatesAsync(owner);
                    break;

                case PerejegMenuAction.PrintBlank:
                    await PrintPerejegBlankAsync(owner);
                    break;
            }
        }
    }

    private static async Task OpenPerejegDailyCalculationAsync(Window owner)
    {
        var startVm = new PerejegStartViewModel();
        var dialog = new PerejegStartWindow(startVm);
        var result = await dialog.ShowDialog<PerejegStartResult?>(owner);

        if (result is null)
            return;

        if (owner.DataContext is MainViewModel main)
            main.NavigateTo(new PerejegEditViewModel(result, main.GoToMenu, main.NavigateTo));
    }

    private static async Task<bool> TryShowPerejegDailyReportAsync(Window owner)
    {
        var dateVm = new PerejegDateViewModel(
            "Печать суточных пережогов",
            "Укажите дату расчёта");
        var dateDialog = new PerejegDateWindow(dateVm);
        var date = await dateDialog.ShowDialog<DateTime?>(owner);
        if (date is null)
            return false;

        var report = PerejegStore.TryLoadReport(date.Value);
        if (report is null)
        {
            await ShowMessageAsync(owner, "Данные за это число отсутствуют.");
            return false;
        }

        if (owner.DataContext is not MainViewModel main)
            return false;

        main.NavigateTo(new PerejegReportViewModel(
            report,
            main.GoToMenu,
            backButtonText: "← В меню"));
        return true;
    }

    private static async Task<bool> TryShowPerejegCumulativeReportAsync(Window owner)
    {
        var rangeVm = new PerejegRangeViewModel();
        var rangeDialog = new PerejegRangeWindow(rangeVm);
        var range = await rangeDialog.ShowDialog<(DateTime From, DateTime To)?>(owner);
        if (range is null)
            return false;

        var report = PerejegStore.TryBuildCumulativeReport(range.Value.From, range.Value.To);
        if (report is null)
        {
            await ShowMessageAsync(owner, "Данные за этот период отсутствуют.");
            return false;
        }

        if (owner.DataContext is not MainViewModel main)
            return false;

        main.NavigateTo(new PerejegReportViewModel(
            report,
            main.GoToMenu,
            backButtonText: "← В меню"));
        return true;
    }

    private static async Task ShowPerejegDatesAsync(Window owner)
    {
        var datesVm = new PerejegDatesViewModel();
        var datesWindow = new PerejegDatesWindow(datesVm);
        await datesWindow.ShowDialog(owner);
    }

    private static Task PrintPerejegBlankAsync(Window owner)
    {
        PerejegPrintService.PrintBlankForm();
        return Task.CompletedTask;
    }

    private static async Task<bool?> ShowConfirmAsync(Window owner, string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#F4F8FA")),
        };

        bool? result = null;

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16),
        };

        var yes = new Button
        {
            Content = "Да",
            Classes = { "primary" },
            MinWidth = 90,
            Margin = new Thickness(0, 0, 8, 0),
        };
        yes.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };

        var no = new Button
        {
            Content = "Нет",
            Classes = { "exit" },
            MinWidth = 90,
        };
        no.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        buttons.Children.Add(yes);
        buttons.Children.Add(no);

        var panel = new DockPanel { Margin = new Thickness(24) };
        DockPanel.SetDock(buttons, Dock.Bottom);
        panel.Children.Add(buttons);
        panel.Children.Add(text);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
        return result;
    }

    private static async Task ShowMessageAsync(Window owner, string message)
    {
        var dialog = new Window
        {
            Title = "ОПТО",
            Width = 380,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#F4F8FA")),
        };

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16),
        };

        var ok = new Button
        {
            Content = "OK",
            Classes = { "primary" },
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 90,
        };
        ok.Click += (_, _) => dialog.Close();

        var panel = new DockPanel { Margin = new Thickness(24) };
        DockPanel.SetDock(ok, Dock.Bottom);
        panel.Children.Add(ok);
        panel.Children.Add(text);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }

    private static void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
