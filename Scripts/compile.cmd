@echo off

dotnet build --no-incremental .\EfficientBaseRepair.csproj

if ERRORLEVEL 1 exit /b 1

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
