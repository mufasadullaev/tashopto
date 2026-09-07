using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class BaxtaDatesWindow : Window
{
    public BaxtaDatesWindow()
    {
        InitializeComponent();
    }

    public BaxtaDatesWindow(BaxtaDatesViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = () => Close();
    }
}
