using System;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opto.Services.Database;

namespace Opto.ViewModels;

public partial class BackupViewModel : ViewModelBase
{
    private readonly Action _goBack;

    [ObservableProperty]
    private string? _statusMessage;

    public ICommand ExportBackupCommand { get; }
    public ICommand BackCommand { get; }

    public BackupViewModel(Action goBack)
    {
        _goBack = goBack;
        ExportBackupCommand = new RelayCommand(ExportBackup);
        BackCommand = new RelayCommand(goBack);
    }

    private void ExportBackup()
    {
        try
        {
            var dbPath = OptoDatabase.DatabasePath;
            if (!File.Exists(dbPath))
            {
                StatusMessage = "Файл базы данных не найден.";
                return;
            }

            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Opto_Backups");
            Directory.CreateDirectory(backupDir);

            var backupFile = Path.Combine(
                backupDir,
                $"opto_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            File.Copy(dbPath, backupFile, overwrite: true);
            StatusMessage = $"Резервная копия создана: {backupFile}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка создания бэкапа: {ex.Message}";
        }
    }
}
