using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class O2GolRangeWindow : Window
{
    public O2GolRangeWindow()
    {
        InitializeComponent();
    }

    public O2GolRangeWindow(O2GolRangeViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = result => Close(result);
    }
}
