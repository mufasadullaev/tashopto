using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Services.Database;

namespace Opto.ViewModels;

public partial class ServiceViewModel : ViewModelBase
{
    private readonly Action _goBack;

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand VacuumCommand { get; }
    public ICommand IntegrityCheckCommand { get; }
    public ICommand BackCommand { get; }

    public ServiceViewModel(Action goBack)
    {
        _goBack = goBack;

        VacuumCommand = new RelayCommand(RunVacuum);
        IntegrityCheckCommand = new RelayCommand(RunIntegrityCheck);
        BackCommand = new RelayCommand(goBack);
    }

    private void RunVacuum()
    {
        try
        {
            using var connection = OptoDatabase.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "VACUUM;";
            command.ExecuteNonQuery();
            StatusMessage = "Оптимизация базы данных (VACUUM) успешно завершена.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка оптимизации: {ex.Message}";
        }
    }

    private void RunIntegrityCheck()
    {
        try
        {
            using var connection = OptoDatabase.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = command.ExecuteScalar()?.ToString();
            StatusMessage = $"Проверка целостности БД: {result}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка проверки целостности: {ex.Message}";
        }
    }
}
