@echo off
rem Trách nhiệm: chạy NetCafe.Client bằng Windows PowerShell khi máy không có pwsh.
rem Liên kết: run-station.ps1 đọc credential station và computer_code.
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-station.ps1" %*
exit /b %ERRORLEVEL%
