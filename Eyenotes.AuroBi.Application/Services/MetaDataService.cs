using Dapper;
using Eyenotes.AuroBi.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Application.Services
{
    public interface IMetaDataService
    {
        Task<IEnumerable<string>> GetAllTableNamesAsync();
        Task<IEnumerable<object>> GetTableColumnsAsync(string fullTableName); // expects "schema.table"
        Task<IEnumerable<dynamic>> GetTableDataAsync(string fullTableName);   // expects "schema.table"
        Task<IEnumerable<dynamic>> RunQueryAsync(string query);
    }

    public class MetaDataService : IMetaDataService
    {
        private readonly IMetaDataRepository _repository;
        private readonly ILogger<MetaDataService> _logger;

        public MetaDataService(IMetaDataRepository repository, ILogger<MetaDataService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> GetAllTableNamesAsync()
        {
            try
            {
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var tableNames = await connection.QueryAsync<string>(SqlQueries.GetAllTables);
                _logger.LogInformation("Retrieved {Count} tables.", tableNames.Count());
                return tableNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch table names.");
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetTableColumnsAsync(string fullTableName)
        {
            if (!TryParseSchemaTable(fullTableName, out var schema, out var table))
                throw new ArgumentException("Invalid format. Use 'schema.table'.");

            if (!await IsTableNameValidAsync(fullTableName))
            {
                _logger.LogWarning("Attempt to access invalid table metadata: {TableName}", fullTableName);
                throw new ArgumentException("Invalid table name.");
            }

            try
            {
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var columns = await connection.QueryAsync(SqlQueries.GetColumnsByTable, new { TableName = table, SchemaName = schema });
                return columns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch columns for table {TableName}", fullTableName);
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> GetTableDataAsync(string fullTableName)
        {
            if (!TryParseSchemaTable(fullTableName, out var schema, out var table))
                throw new ArgumentException("Invalid format. Use 'schema.table'.");

            if (!await IsTableNameValidAsync(fullTableName))
            {
                _logger.LogWarning("Attempt to access invalid table: {TableName}", fullTableName);
                throw new ArgumentException("Invalid table name.");
            }

            try
            {
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var query = SqlQueries.GetDataFromTable(schema, table);
                _logger.LogInformation("Running data query: {Query}", query);

                var data = await connection.QueryAsync(query);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data for table {TableName}", fullTableName);
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> RunQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || !query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejected non-SELECT query: {Query}", query);
                throw new InvalidOperationException("Only SELECT queries are allowed.");
            }

            try
            {
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var data = await connection.QueryAsync(query);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query failed: {Query}", query);
                throw;
            }
        }

        private async Task<bool> IsTableNameValidAsync(string fullTableName)
        {
            var validTableNames = await GetAllTableNamesAsync();
            return validTableNames.Contains(fullTableName, StringComparer.OrdinalIgnoreCase);
        }

        private bool TryParseSchemaTable(string fullTableName, out string schema, out string table)
        {
            schema = string.Empty;
            table = string.Empty;

            var parts = fullTableName?.Split('.', 2);
            if (parts?.Length != 2) return false;

            schema = parts[0];
            table = parts[1];
            return true;
        }
    }

    #region SQL queries
    public static class SqlQueries
    {
        public const string GetAllTables = @"
            SELECT TABLE_SCHEMA + '.' + TABLE_NAME AS FullTableName
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'";

        public const string GetColumnsByTable = @"
            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = @SchemaName";

        public static string GetDataFromTable(string schemaName, string tableName)
        {
            return $"SELECT * FROM [{schemaName}].[{tableName}]";
        }
    }
    #endregion
}
