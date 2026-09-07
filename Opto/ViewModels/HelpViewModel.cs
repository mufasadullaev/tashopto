using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Opto.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    public ICommand BackCommand { get; }

    public HelpViewModel(Action goBack)
    {
        BackCommand = new RelayCommand(goBack);
    }
}
