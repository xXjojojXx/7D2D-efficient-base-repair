@echo off

call "%~dp0..\Scripts\release.cmd"

if %ERRORLEVEL% neq 0 exit /b 1

IF NOT DEFINED PATH_7D2D_DEDI (
    echo env variable 'PATH_7D2D_DEDI' must be defined.
    exit /b 1
)

set MOD_PATH="%PATH_7D2D_DEDI%\Mods\EfficientBaseRepair"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

cd %MOD_PATH%\..

7z.exe x "%~dp0..\EfficientBaseRepair.zip" > nul

cd ..

taskkill /IM 7DaysToDieServer.exe /F

call "startdedicated.bat"