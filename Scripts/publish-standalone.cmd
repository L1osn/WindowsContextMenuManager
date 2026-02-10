@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "ROOT=%SCRIPT_DIR%.."
cd /d "%ROOT%"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%publish-standalone.ps1"
endlocal
