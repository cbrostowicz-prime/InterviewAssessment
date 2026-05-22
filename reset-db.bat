@echo off
REM Drops the OrderHistory database so the API reseeds on next startup.
REM Run with the API stopped; restart `dotnet run` afterwards to repopulate.

setlocal

set CONTAINER=orderhistory-sqlserver
set SA_PASSWORD=Local_Dev_Pass1!

echo Checking that %CONTAINER% is running...
docker ps --filter "name=^/%CONTAINER%$" --filter "status=running" --format "{{.Names}}" | findstr /B /L "%CONTAINER%" >nul
if errorlevel 1 (
    echo Container %CONTAINER% is not running. Starting docker compose...
    docker compose up -d sqlserver
    if errorlevel 1 (
        echo Failed to start sqlserver container.
        exit /b 1
    )
    echo Waiting 15s for SQL Server to accept connections...
    timeout /t 15 /nobreak >nul
)

echo Dropping OrderHistory database...
docker exec %CONTAINER% /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "%SA_PASSWORD%" -C -No -Q "IF DB_ID('OrderHistory') IS NOT NULL BEGIN ALTER DATABASE OrderHistory SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE OrderHistory; END"
if errorlevel 1 (
    echo Failed to drop database. Is the API still running and holding connections?
    exit /b 1
)

echo.
echo Database dropped. Start the API to reseed:
echo     dotnet run --project backend/src/OrderHistory.Api
echo.

endlocal
