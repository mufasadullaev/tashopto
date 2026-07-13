using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Opto.ViewModels;

public class PlaceholderViewModel : ViewModelBase
{
    public string Title { get; }

    public ICommand GoBackCommand { get; }

    public PlaceholderViewModel(string title, System.Action goBack)
    {
        Title = title;
        GoBackCommand = new RelayCommand(goBack);
    }
}
