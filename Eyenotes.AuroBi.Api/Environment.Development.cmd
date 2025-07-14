@echo off
REM --- Set Environment to Development ---
SETX ASPNETCORE_ENVIRONMENT "Development" /M

REM --- Set SQL Server Connection String ---
SETX Eyenotes20_EmrConnection "Server=localhost,1433;Database=EmrLocal;User Id=sa;Password=Pass@123;Encrypt=False;MultipleActiveResultSets=True;" /M

REM --- Set Postgres Connection String ---
SETX Eyenotes20_AuroBiConnection "Host=localhost;Port=5432;Database=aurobi_dev;Username=postgres;Password=12345678;" /M

echo Environment variables set successfully for Development.
pause
