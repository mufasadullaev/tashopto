using Avalonia.Controls;
using Avalonia.Interactivity;
using Opto.ViewModels;

namespace Opto.Views;

public partial class WyrabotkaStartWindow : Window
{
    public WyrabotkaStartWindow()
    {
        InitializeComponent();
    }

    public WyrabotkaStartWindow(WyrabotkaStartViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = result => Close(result);
    }

    private void OnCalculationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WyrabotkaStartViewModel vm)
            vm.SelectCalculation();
    }

    private void OnRecalculationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WyrabotkaStartViewModel vm)
            vm.SelectRecalculation();
    }
}
