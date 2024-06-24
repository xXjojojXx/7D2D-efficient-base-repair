@echo off

call "%~dp0\compile.cmd"

if ERRORLEVEL 1 exit /b 1

set MOD_PATH="%PATH_7D2D%\Mods\EfficientBaseRepair"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

cd %MOD_PATH%\..

7z.exe x "%~dp0..\EfficientBaseRepair.zip" > nul

taskkill /IM 7DaysToDie.exe /F >nul 2>&1

@REM start "" "%PATH_7D2D%\7DaysToDie" -force-d3d11 -disablenativeinput -nogs -noeac

exit /b 0