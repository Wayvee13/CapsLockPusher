@echo off
setlocal

echo Building smaller framework-dependent CapsLockPusher.exe...
echo This requires .NET Desktop Runtime 8 installed on the target PC.
echo.

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed.
    echo Install .NET SDK 8 from:
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

dotnet publish CapsLockPusher.csproj -c Release -r win-x64 --self-contained false

echo.
echo Done.
echo EXE location:
echo bin\Release\net8.0-windows\win-x64\publish\CapsLockPusher.exe
echo.
pause
