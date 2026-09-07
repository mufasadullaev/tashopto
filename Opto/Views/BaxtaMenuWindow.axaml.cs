using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class BaxtaMenuWindow : Window
{
    public BaxtaMenuWindow()
    {
        InitializeComponent();
    }

    public BaxtaMenuWindow(BaxtaMenuViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = action => Close(action);
    }
}
