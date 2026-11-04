@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check_all.ps1" %*
exit /b %ERRORLEVEL%
