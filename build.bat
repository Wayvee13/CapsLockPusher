@echo off
setlocal

echo Building CapsLockPusher UI...
echo.

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed.
    echo Install .NET SDK 8 from:
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

dotnet publish CapsLockPusher.csproj -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true

echo.
echo Done.
echo EXE location:
echo bin\Release\net8.0-windows\win-x64\publish\CapsLockPusher.exe
echo.
pause
