using Avalonia.Controls;
using Avalonia.Interactivity;
using Opto.ViewModels;

namespace Opto.Views;

public partial class BaxtaStartWindow : Window
{
    public BaxtaStartWindow()
    {
        InitializeComponent();
    }

    public BaxtaStartWindow(BaxtaStartViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = result => Close(result);
    }

    private void OnCalculationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BaxtaStartViewModel vm)
            vm.SelectCalculation();
    }

    private void OnRecalculationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BaxtaStartViewModel vm)
            vm.SelectRecalculation();
    }
}
