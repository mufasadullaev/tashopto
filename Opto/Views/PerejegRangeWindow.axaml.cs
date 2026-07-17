using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class PerejegRangeWindow : Window
{
    public PerejegRangeWindow()
    {
        InitializeComponent();
    }

    public PerejegRangeWindow(PerejegRangeViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = result => Close(result);
    }
}
