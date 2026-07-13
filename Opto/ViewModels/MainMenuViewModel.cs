using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Opto.Models;

namespace Opto.ViewModels;

public class MainMenuViewModel : ViewModelBase
{
    public ObservableCollection<MenuSection> Sections { get; }

    public ICommand ExitCommand { get; }

    public MainMenuViewModel(AsyncRelayCommand<MenuAction> openModule, ICommand exitCommand)
    {
        ExitCommand = exitCommand;
        Sections =
        [
            CreateSection("Ежедневно", openModule,
            [
                new MenuAction { Id = "baxta", Title = "Ежедневный расчёт по вахтам" },
                new MenuAction { Id = "perejeg", Title = "Пережоги топлива" },
                new MenuAction { Id = "wyrabotka", Title = "Выработка электроэнергии" },
            ]),
            CreateSection("Еженедельно", openModule,
            [
                new MenuAction { Id = "o2gol", Title = "Сводный расчёт по вахтам" },
            ]),
            CreateSection("В конце месяца и по запросу", openModule,
            [
                new MenuAction { Id = "akt", Title = "Акт по выработке электроэнергии" },
                new MenuAction { Id = "selektor", Title = "Расчёт для селектора" },
                new MenuAction { Id = "newmonth", Title = "Переход на новый месяц", IsEmphasized = true },
            ]),
            CreateSection("По мере необходимости", openModule,
            [
                new MenuAction { Id = "backup", Title = "Резервное копирование данных" },
                new MenuAction { Id = "manual", Title = "Технологическая инструкция" },
                new MenuAction { Id = "service", Title = "Служебные функции" },
                new MenuAction { Id = "help", Title = "HELP" },
            ]),
        ];
    }

    private static MenuSection CreateSection(string title, ICommand open, MenuAction[] items)
    {
        foreach (var item in items)
            item.Command = open;

        return new MenuSection { Title = title, Items = items };
    }
}
