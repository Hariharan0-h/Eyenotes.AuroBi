using Dapper;
using Eyenotes.AuroBi.Domain.Data;
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
        private readonly IDynamicDbContext _dynamicDbContext;
        private readonly ILogger<MetaDataService> _logger;

        public MetaDataService(IMetaDataRepository repository, IDynamicDbContext dynamicDbContext, ILogger<MetaDataService> logger)
        {
            _repository = repository;
            _dynamicDbContext = dynamicDbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> GetAllTableNamesAsync()
        {
            try
            {
                var provider = _dynamicDbContext.GetProvider();
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var sql = SqlQueryFactory.GetAllTables(provider);
                var tableNames = await connection.QueryAsync<string>(sql);
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
                _logger.LogWarning("Invalid table: {TableName}", fullTableName);
                throw new ArgumentException("Invalid table name.");
            }

            try
            {
                var provider = _dynamicDbContext.GetProvider();
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var sql = SqlQueryFactory.GetColumns(provider);
                return await connection.QueryAsync(sql, new { TableName = table, SchemaName = schema });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch columns.");
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> GetTableDataAsync(string fullTableName)
        {
            if (!TryParseSchemaTable(fullTableName, out var schema, out var table))
                throw new ArgumentException("Invalid format. Use 'schema.table'.");

            if (!await IsTableNameValidAsync(fullTableName))
            {
                _logger.LogWarning("Invalid table: {TableName}", fullTableName);
                throw new ArgumentException("Invalid table name.");
            }

            try
            {
                var provider = _dynamicDbContext.GetProvider();
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var sql = SqlQueryFactory.GetData(provider, schema, table);
                return await connection.QueryAsync(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data.");
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> RunQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || !query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only SELECT queries are allowed.");

            try
            {
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                return await connection.QueryAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query failed.");
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
    public static class SqlQueryFactory
    {
        public static string GetAllTables(string provider)
        {
            return provider switch
            {
                "SqlServer" => @"
                    SELECT TABLE_SCHEMA + '.' + TABLE_NAME AS FullTableName
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = 'BASE TABLE'",
                "PostgreSQL" => @"
                    SELECT table_schema || '.' || table_name AS FullTableName
                    FROM information_schema.tables
                    WHERE table_type='BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema')",
                _ => throw new NotSupportedException("Unsupported database provider")
            };
        }

        public static string GetColumns(string provider)
        {
            return provider switch
            {
                "SqlServer" => @"
                    SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = @SchemaName",
                "PostgreSQL" => @"
                    SELECT column_name AS COLUMN_NAME, data_type AS DATA_TYPE, character_maximum_length AS CHARACTER_MAXIMUM_LENGTH, is_nullable AS IS_NULLABLE
                    FROM information_schema.columns
                    WHERE table_name = @TableName AND table_schema = @SchemaName",
                _ => throw new NotSupportedException("Unsupported database provider")
            };
        }

        public static string GetData(string provider, string schemaName, string tableName)
        {
            return provider switch
            {
                "SqlServer" => $"SELECT * FROM [{schemaName}].[{tableName}]",
                "PostgreSQL" => $"SELECT * FROM \"{schemaName}\".\"{tableName}\"",
                _ => throw new NotSupportedException("Unsupported database provider")
            };
        }
    }
    #endregion
}
