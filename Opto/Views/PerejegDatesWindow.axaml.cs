using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class PerejegDatesWindow : Window
{
    public PerejegDatesWindow()
    {
        InitializeComponent();
    }

    public PerejegDatesWindow(PerejegDatesViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = () => Close();
    }
}
