@echo off
echo ==========================================
echo   ConsignmentGenie - Full Stack Launcher
echo ==========================================
echo.
echo This will start both the API and Frontend
echo API: https://localhost:7042
echo Frontend: http://localhost:4200
echo.

echo Cleaning up any existing processes...
echo.

:: Kill any existing dotnet processes (API) - Windows
taskkill /f /im dotnet.exe 2>nul
if %ERRORLEVEL% equ 0 echo - Stopped existing Windows API processes

:: Kill any existing node processes (Angular dev server) - Windows
taskkill /f /im node.exe 2>nul
if %ERRORLEVEL% equ 0 echo - Stopped existing Windows Node.js processes

:: Kill any existing ng processes
wmic process where "commandline like '%%ng serve%%'" delete 2>nul

:: Kill any existing dotnet run processes in WSL (more targeted)
echo - Cleaning WSL processes...
wsl pkill -f "dotnet run" 2>nul
wsl pkill -f "ng serve" 2>nul

:: Wait for cleanup to complete
echo - Waiting for cleanup to complete...
timeout /t 3 /nobreak > nul

echo Process cleanup complete.
echo.

:: Check if we're in the right directory
if not exist "ConsignmentGenie_API\ConsignmentGenie.API\ConsignmentGenie.API.csproj" (
    echo ERROR: API project not found!
    echo Please run this script from the ConsignmentGenie root directory.
    pause
    exit /b 1
)

if not exist "ConsignmentGenie_UI\src" (
    echo ERROR: UI project not found!
    echo Please run this script from the ConsignmentGenie root directory.
    pause
    exit /b 1
)

echo Starting API server in background...
echo.

:: Start API in a new window using WSL (to access User Secrets)
start "ConsignmentGenie API" cmd /k "wsl bash /mnt/c/Projects/ConsignmentGenie/start-api-wsl.sh"

:: Wait a moment for API to start
timeout /t 5 /nobreak > nul

echo Starting Angular frontend...
echo.

:: Navigate to frontend and start
cd ConsignmentGenie_UI

:: Check if node_modules exist
if not exist node_modules (
    echo Installing npm dependencies...
    npm install
    if %ERRORLEVEL% neq 0 (
        echo ERROR: npm install failed!
        pause
        exit /b 1
    )
)

echo Building and starting Angular development server with source maps...
echo This will launch Chrome with debugging enabled.
echo.

:: Start Angular dev server with source maps and launch Chrome with dev tools optimizations
start "Chrome DevTools" cmd /c "timeout /t 8 && start chrome --auto-open-devtools-for-tabs --enable-precise-memory-info --js-flags=--expose-gc http://localhost:4200"
ng serve --configuration development

echo.
echo Both services have been started!
echo - API: https://localhost:7042 (check the API window)
echo - Frontend: http://localhost:4200
echo.
echo STOP SERVICES:
echo - Frontend: Close this window or press Ctrl+C
echo - API: Close the API window OR run kill-api.bat
echo.
pause