@echo off
REM --- Set Environment to Qa ---
SETX ASPNETCORE_ENVIRONMENT "Qa" /M

REM --- Set SQL Server Connection String ---
SETX Eyenotes20_EmrConnection "Server=localhost,1433;Database=EmrLocal;User Id=sa;Password=Pass@123;Trusted_Connection=True;Encrypt=False;" /M

echo Environment variables set successfully for Qa.
pause
