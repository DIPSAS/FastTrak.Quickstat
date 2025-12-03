@echo off
REM Build script for QuickStat.dpr using dcc32

echo Building QuickStat...

REM Set the project file
set PROJECT=QuickStat.dpr

REM Set Delphi paths
set DELPHI_ROOT=C:\Program Files (x86)\Embarcadero\Studio\37.0
set DELPHI_LIB=%DELPHI_ROOT%\lib\win32\release
set DELPHI_SOURCE=%DELPHI_ROOT%\source
set RAIZE_PATH=C:\Users\Public\Documents\Embarcadero\Studio\37.0\CatalogRepository\BonusKSVC\8.0.1\Source

REM Build the project with FastTrak in the search path
dcc32 -NSSystem;Xml;Data;Datasnap;Web;Soap;Vcl;Vcl.Imaging;Vcl.Touch;Vcl.Samples;Vcl.Shell;VCLTee;Winapi -U"FastTrak;Spring;%RAIZE_PATH%;%DELPHI_LIB%" -I"FastTrak" -R"FastTrak;%RAIZE_PATH%" %PROJECT%

if %ERRORLEVEL% EQU 0 (
    echo Build successful!
) else (
    echo Build failed with error code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)
