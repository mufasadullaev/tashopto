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

        var startVm = new BaxtaStartViewModel();
        var dialog = new BaxtaStartWindow(startVm);
        var result = await dialog.ShowDialog<BaxtaStartResult?>(owner);

        if (result is null)
            return;

        NavigateTo(new BaxtaEditViewModel(result, GoToMenu, NavigateTo));
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
                    await PrintPerejegDailyAsync(owner);
                    break;

                case PerejegMenuAction.PrintCumulative:
                    await PrintPerejegCumulativeAsync(owner);
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

    private static async Task PrintPerejegDailyAsync(Window owner)
    {
        var dateVm = new PerejegDateViewModel(
            "Печать суточных пережогов",
            "Укажите дату расчёта");
        var dateDialog = new PerejegDateWindow(dateVm);
        var date = await dateDialog.ShowDialog<DateTime?>(owner);
        if (date is null)
            return;

        var report = PerejegStore.TryLoadReport(date.Value);
        if (report is null)
        {
            await ShowMessageAsync(owner, "Данные за это число отсутствуют.");
            return;
        }

        PerejegPrintService.Print(report);
    }

    private static async Task PrintPerejegCumulativeAsync(Window owner)
    {
        var rangeVm = new PerejegRangeViewModel();
        var rangeDialog = new PerejegRangeWindow(rangeVm);
        var range = await rangeDialog.ShowDialog<(DateTime From, DateTime To)?>(owner);
        if (range is null)
            return;

        var report = PerejegStore.TryBuildCumulativeReport(range.Value.From, range.Value.To);
        if (report is null)
        {
            await ShowMessageAsync(owner, "Данные за этот период отсутствуют.");
            return;
        }

        PerejegPrintService.Print(report);
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
