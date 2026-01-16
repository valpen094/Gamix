@echo off
setlocal

echo [1/2] Publishing application...
powershell -ExecutionPolicy Bypass -File "%~dp0publish.ps1"
if %ERRORLEVEL% neq 0 (
    echo Publish failed.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Compiling Installer (Inno Setup)...
echo Looking for ISCC.exe...

set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
    where iscc.exe >nul 2>nul
    if %ERRORLEVEL% equ 0 (
        set "ISCC=iscc.exe"
    ) else (
        echo ERROR: Inno Setup 6 ^(ISCC.exe^) was not found.
        echo Please install it from: https://jrsoftware.org/isdl.php
        echo If already installed, make sure it's in your PATH or at the default location.
        pause
        exit /b 1
    )
)

echo using: "%ISCC%"
"%ISCC%" "%~dp0..\Setup\Gamix.iss"

if %ERRORLEVEL% neq 0 (
    echo.
    echo Installer compilation failed.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo Done! Installer is in: %~dp0..\Output
echo ========================================
pause
