using Avalonia.Controls;
using Avalonia.Interactivity;
using Opto.ViewModels;

namespace Opto.Views;

public partial class PerejegStartWindow : Window
{
    public PerejegStartWindow()
    {
        InitializeComponent();
    }

    public PerejegStartWindow(PerejegStartViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = result => Close(result);
    }

    private void OnCalculationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PerejegStartViewModel vm)
            vm.SelectCalculation();
    }

    private void OnRecalculationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PerejegStartViewModel vm)
            vm.SelectRecalculation();
    }
}
