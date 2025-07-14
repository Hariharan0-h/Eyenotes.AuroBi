using Eyenotes.AuroBi.Domain.Data;
using Eyenotes.AuroBi.Domain.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.SqlClient;

namespace Eyenotes.AuroBi.Domain.Repositories
{
    public interface IDataSourceRepository
    {
        Task<string> SetSqlServerConnectionAsync(SqlConnectionCredentials creds);
        Task<string> SetPostgresConnectionAsync(PostgresConnectionCredentials creds);
        Task<string> ConnectToDefaultPostgresAsync();
    }

    public class DataSourceRepository : IDataSourceRepository
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<DataSourceRepository> _logger;

        public DataSourceRepository(ILogger<DataSourceRepository> logger, IConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public async Task<string> SetSqlServerConnectionAsync(SqlConnectionCredentials creds)
        {
            var connectionString = $"Server={creds.Host},{creds.Port};Database={creds.Database};User Id={creds.Username};Password={creds.Password};Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;Command Timeout=60;";

            try
            {
                var result = await _connectionManager.SetConnectionAsync(connectionString, "SqlServer");
                _logger.LogInformation("SQL Server connection attempt: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Server connection failed");
                return $"SQL Server connection failed: {ex.Message}";
            }
        }

        public async Task<string> SetPostgresConnectionAsync(PostgresConnectionCredentials creds)
        {
            var connectionString = $"Host={creds.Host};Port={creds.Port};Username={creds.Username};Password={creds.Password};Database={creds.Database};Timeout=30;Command Timeout=60;Pooling=true;MinPoolSize=1;MaxPoolSize=20;";

            try
            {
                var result = await _connectionManager.SetConnectionAsync(connectionString, "PostgreSQL");
                _logger.LogInformation("PostgreSQL connection attempt: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL connection failed");
                return $"PostgreSQL connection failed: {ex.Message}";
            }
        }

        public async Task<string> ConnectToDefaultPostgresAsync()
        {
            var envConn = Environment.GetEnvironmentVariable("Eyenotes20_AuroBiConnection");
            if (string.IsNullOrEmpty(envConn))
                return "PostgreSQL connection string not found in environment variables.";

            try
            {
                var result = await _connectionManager.SetConnectionAsync(envConn, "PostgreSQL");
                _logger.LogInformation("Default PostgreSQL connection attempt: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Default PostgreSQL connection failed");
                return $"Default PostgreSQL connection failed: {ex.Message}";
            }
        }
    }
}