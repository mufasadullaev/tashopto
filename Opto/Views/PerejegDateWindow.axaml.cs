using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class PerejegDateWindow : Window
{
    public PerejegDateWindow()
    {
        InitializeComponent();
    }

    public PerejegDateWindow(PerejegDateViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = result => Close(result);
    }
}
