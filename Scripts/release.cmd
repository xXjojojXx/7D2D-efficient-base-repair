@echo off

call ".\Scripts\compile.cmd"

if %ERRORLEVEL% neq 0 exit /b 1

if exist "EfficientBaseRepair.zip" DEL "EfficientBaseRepair.zip"

if exist ".\EfficientBaseRepair" rmdir ".\EfficientBaseRepair" /s /q

MKDIR .\EfficientBaseRepair

xcopy Config EfficientBaseRepair\Config\ /s > nul
xcopy *.dll EfficientBaseRepair\ > nul
xcopy *.md EfficientBaseRepair\ > nul
xcopy ModInfo.xml EfficientBaseRepair\ > nul

7z.exe a "EfficientBaseRepair.zip" EfficientBaseRepair > nul

rmdir ".\EfficientBaseRepair" /s /q

DEL EfficientBaseRepair.dll
DEL EfficientBaseRepair.pdb

set MOD_PATH="%PATH_7D2D%\Mods\EfficientBaseRepair"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

mkdir %MOD_PATH%

cd "%PATH_7D2D%\Mods"

7z.exe x "%~dp0..\EfficientBaseRepair.zip" > nul
