using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class BaxtaCopyWindow : Window
{
    public BaxtaCopyWindow()
    {
        InitializeComponent();
    }

    public BaxtaCopyWindow(BaxtaCopyViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = ok => Close(ok);
    }
}
