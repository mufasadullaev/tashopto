using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class PerejegMenuWindow : Window
{
    public PerejegMenuWindow()
    {
        InitializeComponent();
    }

    public PerejegMenuWindow(PerejegMenuViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = result => Close(result);
    }
}
