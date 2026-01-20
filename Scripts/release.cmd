@echo off
setlocal

echo Publishing application...
powershell -ExecutionPolicy Bypass -File "%~dp0publish.ps1"
if %ERRORLEVEL% neq 0 (
    echo Publish failed.
    pause
    exit /b %ERRORLEVEL%
)

echo Copying LICENSE...
copy /Y "%~dp0..\LICENSE" "%~dp0..\publish\"

echo Copying THIRD-PARTY-NOTICES.txt...
copy /Y "%~dp0..\THIRD-PARTY-NOTICES.txt" "%~dp0..\publish\"

echo.
echo ========================================
echo Done!
echo ========================================
