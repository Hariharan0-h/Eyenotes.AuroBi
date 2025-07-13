using ClosedXML.Excel;
using Eyenotes.AuroBi.Domain.Data;
using Eyenotes.AuroBi.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.SqlClient;
using System.Text;

namespace Eyenotes.AuroBi.Domain.Repositories
{
    public interface IDataSourceRepository
    {
        Task<string> GetSqlServerConnectionAsync(SqlConnectionCredentials creds);
        Task<string> GetPostgresConnectionAsync(PostgresConnectionCredentials creds);
        Task<byte[]> GenerateExcelTemplateAsync();
        Task<string> UploadExcelAndCreateTableAsync(IFormFile file);
    }

    public class DataSourceRepository : IDataSourceRepository
    {
        private readonly IDynamicDbContext _dbContext;
        private readonly ILogger<DataSourceRepository> _logger;
        private readonly AuroBiContext _context;

        public DataSourceRepository(ILogger<DataSourceRepository> logger, AuroBiContext context, IDynamicDbContext dbContext)
        {
            _logger = logger;
            _context = context;
            _dbContext = dbContext;
        }

        public async Task<string> GetSqlServerConnectionAsync(SqlConnectionCredentials creds)
        {
            var connectionString = $"Server={creds.Host},{creds.Port};Database={creds.Database};User Id={creds.Username};Password={creds.Password};Encrypt=True;TrustServerCertificate=True;";
            try
            {
                var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                _dbContext.SetConnection(conn);

                _logger.LogInformation("Connected to SQL Server successfully.");
                return "Connected to SQL Server successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Server connection failed.");
                return $"SQL Server connection failed: {ex.Message}";
            }
        }

        public async Task<string> GetPostgresConnectionAsync(PostgresConnectionCredentials creds)
        {
            var connectionString = $"Host={creds.Host};Port={creds.Port};Username={creds.Username};Password={creds.Password};Database={creds.Database};";
            try
            {
                var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                _dbContext.SetConnection(conn);

                _logger.LogInformation("Connected to PostgreSQL successfully.");
                return "Connected to PostgreSQL successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL connection failed.");
                return $"PostgreSQL connection failed: {ex.Message}";
            }
        }

        public async Task<byte[]> GenerateExcelTemplateAsync()
        {
            using var workbook = new XLWorkbook();

            // Sheet 1: Data
            var dataSheet = workbook.Worksheets.Add("Data");
            dataSheet.Cell(1, 1).Value = "my_table";
            dataSheet.Cell(2, 1).Value = "id";
            dataSheet.Cell(2, 2).Value = "name";
            dataSheet.Cell(2, 3).Value = "email";
            dataSheet.Cell(3, 1).Value = "1";
            dataSheet.Cell(3, 2).Value = "John";
            dataSheet.Cell(3, 3).Value = "john@example.com";

            // Sheet 2: Schema
            var schemaSheet = workbook.Worksheets.Add("Schema");
            schemaSheet.Cell(1, 1).Value = "ColumnName";
            schemaSheet.Cell(1, 2).Value = "DataType";
            schemaSheet.Cell(1, 3).Value = "Size";
            schemaSheet.Cell(1, 4).Value = "PrimaryKey";
            schemaSheet.Cell(1, 5).Value = "Nullable";

            schemaSheet.Cell(2, 1).Value = "id";
            schemaSheet.Cell(2, 2).Value = "INT";
            schemaSheet.Cell(2, 3).Value = "";
            schemaSheet.Cell(2, 4).Value = "TRUE";
            schemaSheet.Cell(2, 5).Value = "FALSE";

            schemaSheet.Cell(3, 1).Value = "name";
            schemaSheet.Cell(3, 2).Value = "VARCHAR";
            schemaSheet.Cell(3, 3).Value = "100";
            schemaSheet.Cell(3, 4).Value = "FALSE";
            schemaSheet.Cell(3, 5).Value = "TRUE";

            schemaSheet.Cell(4, 1).Value = "email";
            schemaSheet.Cell(4, 2).Value = "VARCHAR";
            schemaSheet.Cell(4, 3).Value = "255";
            schemaSheet.Cell(4, 4).Value = "FALSE";
            schemaSheet.Cell(4, 5).Value = "TRUE";

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return await Task.FromResult(ms.ToArray());
        }

        public async Task<string> UploadExcelAndCreateTableAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "Invalid file.";

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var dataSheet = workbook.Worksheet("Data");
            var schemaSheet = workbook.Worksheet("Schema");

            if (dataSheet == null || schemaSheet == null)
                return "Missing 'Data' or 'Schema' sheet.";

            string tableName = dataSheet.Cell(1, 1).GetString().Trim();
            if (string.IsNullOrEmpty(tableName))
                return "Table name must be provided in Data!A1.";

            List<string> columnNames = new();
            int colIdx = 1;
            while (!string.IsNullOrWhiteSpace(dataSheet.Cell(2, colIdx).GetString()))
            {
                columnNames.Add(dataSheet.Cell(2, colIdx).GetString().Trim());
                colIdx++;
            }

            if (columnNames.Count == 0)
                return "No column names found in row 2 of Data sheet.";

            if (columnNames.Distinct().Count() != columnNames.Count)
                return "Duplicate column names in Data sheet.";

            var schemaList = new List<(string Column, string DataType, string Size, bool IsPrimaryKey, bool IsNullable)>();
            int schemaRow = 2;
            while (!string.IsNullOrWhiteSpace(schemaSheet.Cell(schemaRow, 1).GetString()))
            {
                string col = schemaSheet.Cell(schemaRow, 1).GetString().Trim();
                string type = schemaSheet.Cell(schemaRow, 2).GetString().Trim().ToUpper();
                string size = schemaSheet.Cell(schemaRow, 3).GetString().Trim();
                string pk = schemaSheet.Cell(schemaRow, 4).GetString().Trim().ToUpper();
                string nullable = schemaSheet.Cell(schemaRow, 5).GetString().Trim().ToUpper();

                if (string.IsNullOrWhiteSpace(col) || string.IsNullOrWhiteSpace(type))
                    return $"Missing column name or data type at row {schemaRow} in Schema sheet.";

                bool isPK = pk == "TRUE" || pk == "YES" || pk == "1";
                bool isNullable = nullable != "FALSE" && nullable != "NO" && nullable != "0";

                schemaList.Add((col, type, size, isPK, isNullable));
                schemaRow++;
            }

            if (schemaList.Count == 0)
                return "No schema definition found in 'Schema' sheet.";

            if (schemaList.Select(s => s.Column).Distinct().Count() != schemaList.Count)
                return "Duplicate column definitions in Schema sheet.";

            var envConnString = Environment.GetEnvironmentVariable("Eyenotes20_AuroBiConnection");
            if (string.IsNullOrEmpty(envConnString))
                return "Environment variable 'Eyenotes20_AuroBiConnection' is missing.";

            var conn = new NpgsqlConnection(envConnString);
            await conn.OpenAsync();
            _dbContext.SetConnection(conn);

            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = $"SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = '{tableName}');";
            var exists = (bool)await checkCmd.ExecuteScalarAsync();
            if (exists)
                return $"Table '{tableName}' already exists.";

            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE \"{tableName}\" (");
            var primaryKeys = new List<string>();

            foreach (var col in schemaList)
            {
                var fullType = string.IsNullOrEmpty(col.Size) ? col.DataType : $"{col.DataType}({col.Size})";
                var nullable = col.IsNullable ? "" : " NOT NULL";
                sb.AppendLine($"    \"{col.Column}\" {fullType}{nullable},");
                if (col.IsPrimaryKey) primaryKeys.Add(col.Column);
            }

            if (primaryKeys.Any())
            {
                sb.AppendLine($"    PRIMARY KEY ({string.Join(", ", primaryKeys.Select(pk => $"\"{pk}\""))})");
            }
            else
            {
                sb.Length -= 3;
            }

            sb.AppendLine(");");

            using var createCmd = conn.CreateCommand();
            createCmd.CommandText = sb.ToString();
            await createCmd.ExecuteNonQueryAsync();

            var pkIndexes = primaryKeys.Select(pk => columnNames.IndexOf(pk)).ToList();
            var insertedPKs = new HashSet<string>();
            var errors = new List<string>();
            int rowIdx = 3;
            var insertCount = 0;

            while (!dataSheet.Row(rowIdx).IsEmpty())
            {
                var values = new List<string>();
                var pkKey = new StringBuilder();

                for (int i = 0; i < columnNames.Count; i++)
                {
                    string val = dataSheet.Cell(rowIdx, i + 1).GetString().Trim();
                    var schemaCol = schemaList.FirstOrDefault(c => c.Column == columnNames[i]);

                    if (!schemaCol.IsNullable && string.IsNullOrEmpty(val))
                        errors.Add($"Row {rowIdx}: NULL not allowed in column '{schemaCol.Column}'.");

                    if (pkIndexes.Contains(i)) pkKey.Append(val).Append("|");
                    values.Add($"'{val.Replace("'", "''")}'");
                }

                string pkComposite = pkKey.ToString();
                if (!string.IsNullOrEmpty(pkComposite) && !insertedPKs.Add(pkComposite))
                    errors.Add($"Row {rowIdx}: Duplicate primary key combination.");

                if (!errors.Any(e => e.StartsWith($"Row {rowIdx}:")))
                {
                    var insertSql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames.Select(c => $"\"{c}\""))}) VALUES ({string.Join(", ", values)});";
                    using var insertCmd = conn.CreateCommand();
                    insertCmd.CommandText = insertSql;
                    try
                    {
                        await insertCmd.ExecuteNonQueryAsync();
                        insertCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowIdx}: DB Insert failed - {ex.Message}");
                    }
                }

                rowIdx++;
            }

            if (errors.Any())
                return $"Table '{tableName}' created, but {errors.Count} data issue(s):\n" + string.Join("\n", errors);

            return $"Table '{tableName}' created and {insertCount} row(s) inserted successfully.";
        }
    }
}