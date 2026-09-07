using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Opto.ViewModels;

public partial class ManualViewModel : ViewModelBase
{
    public ICommand BackCommand { get; }

    public ManualViewModel(Action goBack)
    {
        BackCommand = new RelayCommand(goBack);
    }
}
