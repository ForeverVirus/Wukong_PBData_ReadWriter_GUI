@echo off
setlocal enabledelayedexpansion

cd %~dp0

set repak_exe="%CD%\repak.exe"

for /f "tokens=1,* delims==" %%A in (settings.ini) do (
    if "%%A"=="pak_version" set pak_version=%%B
)

if not defined pak_version (
    echo Pak version not found in settings.ini.
    pause
    exit /b
)

if "%~1"=="" (
    echo No file dropped. Please drag and drop a pak folder onto this batch file.
    pause
    exit /b
)

for %%A in ("%~1") do (
    set "folder_name=%%~nA"
)

set pak_file_name="%folder_name%.pak"

if exist "%pak_file_name%" (
    echo Deleting existing .pak file: %pak_file_name%
    del "%pak_file_name%"
)

%repak_exe% pack --version %pak_version% "%~1"

exit /b
