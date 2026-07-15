@echo off
REM Скрипт для очистки базы данных Opto

powershell -ExecutionPolicy Bypass -File "%~dp0clear-database.ps1"
pause
