@echo off
setlocal

call "%~dp0publish_win-x64.bat" --no-pause
if errorlevel 1 goto :error
call "%~dp0publish_linux-x64.bat" --no-pause
if errorlevel 1 goto :error
call "%~dp0publish_linux-arm64.bat" --no-pause
if errorlevel 1 goto :error
call "%~dp0publish_osx-x64.bat" --no-pause
if errorlevel 1 goto :error
call "%~dp0publish_osx-arm64.bat" --no-pause
if errorlevel 1 goto :error

echo.
echo All Zitie targets published successfully.
if /I not "%CODEX_NO_EXPLORER%"=="1" explorer "%~dp0artifacts\publish"
if /I not "%~1"=="--no-pause" if /I not "%CODEX_NO_PAUSE%"=="1" pause
exit /b 0

:error
set "exit_code=%errorlevel%"
echo.
echo Zitie publishing failed with exit code %exit_code%.
if /I not "%~1"=="--no-pause" if /I not "%CODEX_NO_PAUSE%"=="1" pause
exit /b %exit_code%
