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
            var sheet1 = workbook.Worksheets.Add("Columns");
            sheet1.Cell(1, 1).Value = "ColumnName";
            sheet1.Cell(2, 1).Value = "id";
            sheet1.Cell(3, 1).Value = "name";

            var sheet2 = workbook.Worksheets.Add("Types");
            sheet2.Cell(1, 1).Value = "ColumnName";
            sheet2.Cell(1, 2).Value = "DataType";
            sheet2.Cell(1, 3).Value = "Size";
            sheet2.Cell(2, 1).Value = "id";
            sheet2.Cell(2, 2).Value = "INT";
            sheet2.Cell(2, 3).Value = "";
            sheet2.Cell(3, 1).Value = "name";
            sheet2.Cell(3, 2).Value = "VARCHAR";
            sheet2.Cell(3, 3).Value = "255";

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
            var columnSheet = workbook.Worksheet("Columns");
            var typeSheet = workbook.Worksheet("Types");

            if (columnSheet == null || typeSheet == null)
                return "Template sheets missing.";

            var columns = new List<(string ColumnName, string DataType, string Size)>();
            int row = 2;

            while (!string.IsNullOrWhiteSpace(typeSheet.Cell(row, 1).GetString()))
            {
                var colName = typeSheet.Cell(row, 1).GetString();
                var dataType = typeSheet.Cell(row, 2).GetString().ToUpper();
                var size = typeSheet.Cell(row, 3).GetString();

                columns.Add((colName, dataType, size));
                row++;
            }

            if (!columns.Any()) return "No columns found.";

            var tableName = "uploaded_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE \"{tableName}\" (");

            foreach (var col in columns)
            {
                var type = string.IsNullOrWhiteSpace(col.Size) ? col.DataType : $"{col.DataType}({col.Size})";
                sb.AppendLine($"    \"{col.ColumnName}\" {type},");
            }

            sb.Length -= 3; // remove the last comma
            sb.AppendLine("\n);");

            var sql = sb.ToString();

            try
            {
                await _context.Database.ExecuteSqlRawAsync(sql);
                return $"Table '{tableName}' created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create table.");
                return $"Failed to create table: {ex.Message}";
            }
        }
    }
}
