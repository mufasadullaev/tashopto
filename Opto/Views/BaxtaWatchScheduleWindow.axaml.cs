using Avalonia.Controls;
using Opto.ViewModels;

namespace Opto.Views;

public partial class BaxtaWatchScheduleWindow : Window
{
    public BaxtaWatchScheduleWindow()
    {
        InitializeComponent();
    }

    public BaxtaWatchScheduleWindow(BaxtaWatchScheduleViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Close = () => Close();
    }
}
