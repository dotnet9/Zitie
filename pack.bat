@echo off
setlocal

echo ========================================
echo Packing Zitie.Avalonia...
echo ========================================

dotnet pack src\Zitie.Avalonia\Zitie.Avalonia.csproj -c Release -o artifacts\packages
if errorlevel 1 (
    echo Pack failed.
    exit /b 1
)

echo Pack succeeded: artifacts\packages
