using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;
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
