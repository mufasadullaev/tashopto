# Скрипт для полной очистки базы данных Opto

$dbPath = "$env:LOCALAPPDATA\Opto\opto.db"

Write-Host "=== Очистка базы данных Opto ===" -ForegroundColor Cyan
Write-Host "Путь к БД: $dbPath`n"

# Проверяем, существует ли БД
if (-not (Test-Path $dbPath)) {
    Write-Host "База данных не найдена. Нечего удалять." -ForegroundColor Yellow
    exit 0
}

# Останавливаем процессы Opto, если запущены
$optoProcesses = Get-Process | Where-Object { $_.ProcessName -like "*Opto*" }
if ($optoProcesses) {
    Write-Host "Найдены запущенные процессы Opto. Останавливаем..." -ForegroundColor Yellow
    $optoProcesses | ForEach-Object {
        Write-Host "  Остановка процесса: $($_.ProcessName) (PID: $($_.Id))"
        Stop-Process -Id $_.Id -Force
    }
    Start-Sleep -Milliseconds 500
}

# Удаляем файл БД
try {
    Remove-Item $dbPath -Force
    Write-Host "`n✓ База данных успешно удалена!" -ForegroundColor Green
    Write-Host "При следующем запуске приложения будет создана новая пустая БД." -ForegroundColor Gray
}
catch {
    Write-Host "`n✗ Ошибка при удалении БД: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Проверяем, что БД действительно удалена
if (Test-Path $dbPath) {
    Write-Host "`n✗ БД всё ещё существует. Возможно, файл заблокирован." -ForegroundColor Red
    exit 1
}
else {
    Write-Host "`n=== Готово ===" -ForegroundColor Cyan
}
