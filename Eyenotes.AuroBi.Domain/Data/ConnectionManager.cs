using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Domain.Data
{
    public interface IConnectionManager
    {
        Task<string> SetConnectionAsync(string connectionString, string provider);
        Task<DbConnection> GetConnectionAsync();
        string GetProvider();
        bool IsConnected();
        Task<bool> TestConnectionAsync();
        void ClearConnection();
    }

    public class ConnectionManager : IConnectionManager
    {
        private string _connectionString;
        private string _provider;
        private readonly ILogger<ConnectionManager> _logger;
        private readonly object _lock = new object();

        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
        }

        public async Task<string> SetConnectionAsync(string connectionString, string provider)
        {
            lock (_lock)
            {
                _connectionString = connectionString;
                _provider = provider;
            }

            var testResult = await TestConnectionAsync();
            return testResult ? $"Connected to {provider} successfully." : $"Failed to connect to {provider}";
        }

        public async Task<DbConnection> GetConnectionAsync()
        {
            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("No connection string set");

            DbConnection connection = _provider switch
            {
                "SqlServer" => new SqlConnection(_connectionString),
                "PostgreSQL" => new NpgsqlConnection(_connectionString),
                _ => throw new NotSupportedException($"Provider {_provider} not supported")
            };

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                return connection;
            }
            catch (Exception ex)
            {
                connection?.Dispose();
                _logger.LogError(ex, "Failed to open database connection");
                throw;
            }
        }

        public string GetProvider() => _provider ?? "Unknown";

        public bool IsConnected() => !string.IsNullOrEmpty(_connectionString);

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = await GetConnectionAsync();
                return connection.State == System.Data.ConnectionState.Open;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return false;
            }
        }

        public void ClearConnection()
        {
            lock (_lock)
            {
                _connectionString = null;
                _provider = null;
            }
        }
    }
}
