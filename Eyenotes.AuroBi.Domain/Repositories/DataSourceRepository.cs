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
        Task<NpgsqlConnection> GetPostgresConnectionAsync();
    }

    public class DataSourceRepository : IDataSourceRepository
    {
        private readonly IDynamicDbContext _dbContext;
        private readonly ILogger<DataSourceRepository> _logger;

        public DataSourceRepository(ILogger<DataSourceRepository> logger, IDynamicDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<string> SetSqlServerConnectionAsync(SqlConnectionCredentials creds)
        {
            var connectionString = $"Server={creds.Host},{creds.Port};Database={creds.Database};User Id={creds.Username};Password={creds.Password};Encrypt=True;TrustServerCertificate=True;";
            try
            {
                var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                _dbContext.SetConnection(conn);
                return "Connected to SQL Server successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Server connection failed.");
                return $"SQL Server connection failed: {ex.Message}";
            }
        }

        public async Task<string> SetPostgresConnectionAsync(PostgresConnectionCredentials creds)
        {
            var connectionString = $"Host={creds.Host};Port={creds.Port};Username={creds.Username};Password={creds.Password};Database={creds.Database};";
            try
            {
                var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                _dbContext.SetConnection(conn);
                return "Connected to PostgreSQL successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL connection failed.");
                return $"PostgreSQL connection failed: {ex.Message}";
            }
        }

        public async Task<NpgsqlConnection> GetPostgresConnectionAsync()
        {
            var envConn = Environment.GetEnvironmentVariable("Eyenotes20_AuroBiConnection");
            if (string.IsNullOrEmpty(envConn))
                throw new InvalidOperationException("PostgreSQL connection string not set in environment variables.");

            var conn = new NpgsqlConnection(envConn);
            await conn.OpenAsync();
            _dbContext.SetConnection(conn);
            return conn;
        }
    }
}
