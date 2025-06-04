@echo off
powershell.exe -ExecutionPolicy Bypass -File "%~dp0SendStockNotifications.ps1" >> "%~dp0logs\stock_notifications_%date:~-4,4%%date:~-10,2%%date:~-7,2%.log" 2>&1 