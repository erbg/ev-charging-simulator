@echo off

REM Charging Point Simulator Start Script for Windows

echo === OCPP Charging Point Simulator ===
echo.

REM Default values
set SERVER_URL=ws://localhost:5000/ocpp
set CHARGE_POINT_ID=CP001
set RFID_CARD=abc123

REM Check if parameters were provided
if not "%1"=="" set SERVER_URL=%1
if not "%2"=="" set CHARGE_POINT_ID=%2
if not "%3"=="" set RFID_CARD=%3

echo Configuration:
echo   Server URL: %SERVER_URL%
echo   Charging Point ID: %CHARGE_POINT_ID%
echo   Test RFID Card: %RFID_CARD%
echo.

REM Ask user which mode to run
echo Select simulation mode:
echo   1) Automatic simulation (runs in background)
echo   2) Interactive simulation (manual control)
echo.
set /p MODE="Enter your choice (1 or 2): "

if "%MODE%"=="1" (
    echo Starting automatic simulation...
    dotnet run --project ecogy.app.chargepoint.simulator.csproj -- "%SERVER_URL%" "%CHARGE_POINT_ID%" "%RFID_CARD%"
) else if "%MODE%"=="2" (
    echo Starting interactive simulation...
    echo Note: You can manually control the charging point using commands.
    echo Type 'h' for help once the simulator starts.
    echo.
    dotnet run --project ..\ecogy.app.chargepoint.interactive\ecogy.app.chargepoint.interactive.csproj -- "%SERVER_URL%" "%CHARGE_POINT_ID%" "%RFID_CARD%"
) else (
    echo Invalid choice. Please run the script again and select 1 or 2.
    pause
    exit /b 1
)

pause