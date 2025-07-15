using ClosedXML.Excel;
using Eyenotes.AuroBi.Domain.Repositories;
using Eyenotes.AuroBi.Domain.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Data.Common;

namespace Eyenotes.AuroBi.Application.Services
{
    public interface IDataSourceService
    {
        Task<byte[]> GenerateExcelTemplateAsync();
        Task<string> UploadExcelAndCreateTableAsync(IFormFile file);
    }

    public class DataSourceService : IDataSourceService
    {
        private readonly IDataSourceRepository _repository;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<DataSourceService> _logger;

        public DataSourceService(
            IDataSourceRepository repository,
            IConnectionManager connectionManager,
            ILogger<DataSourceService> logger)
        {
            _repository = repository;
            _connectionManager = connectionManager;
            _logger = logger;
        }

        public async Task<byte[]> GenerateExcelTemplateAsync()
        {
            using var workbook = new XLWorkbook();

            var dataSheet = workbook.Worksheets.Add("Data");
            dataSheet.Cell(1, 1).Value = "my_table";
            dataSheet.Cell(2, 1).Value = "id";
            dataSheet.Cell(2, 2).Value = "name";
            dataSheet.Cell(2, 3).Value = "email";
            dataSheet.Cell(3, 1).Value = "1";
            dataSheet.Cell(3, 2).Value = "John";
            dataSheet.Cell(3, 3).Value = "john@example.com";

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

            var reportConfigSheet = workbook.Worksheets.Add("Report"); 
            reportConfigSheet.Cell(1, 1).Value = "Id";
            reportConfigSheet.Cell(1, 2).Value = "Report Name";
            reportConfigSheet.Cell(1, 3).Value = "Report Query";

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return await Task.FromResult(ms.ToArray());
        }

        public async Task<string> UploadExcelAndCreateTableAsync(IFormFile file)
        {
            // First, try to connect to the default PostgreSQL database for Excel uploads
            var defaultConnectionResult = await _repository.ConnectToDefaultPostgresAsync();
            if (!defaultConnectionResult.Contains("success", StringComparison.OrdinalIgnoreCase))
            {
                return $"Failed to connect to database: {defaultConnectionResult}";
            }

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            if (!workbook.Worksheets.Contains("Data") || !workbook.Worksheets.Contains("Schema"))
            {
                return "Excel file must contain both 'Data' and 'Schema' worksheets.";
            }

            var dataSheet = workbook.Worksheet("Data");
            var schemaSheet = workbook.Worksheet("Schema");

            string tableName = dataSheet.Cell(1, 1).GetString().Trim();
            if (string.IsNullOrEmpty(tableName))
            {
                return "Table name is required in cell A1 of the Data sheet.";
            }

            // Get column names from Data sheet (row 2)
            List<string> columnNames = new();
            int colIdx = 1;
            while (!string.IsNullOrWhiteSpace(dataSheet.Cell(2, colIdx).GetString()))
            {
                columnNames.Add(dataSheet.Cell(2, colIdx).GetString().Trim());
                colIdx++;
            }

            if (columnNames.Count == 0)
            {
                return "No column names found in row 2 of the Data sheet.";
            }

            // Get schema definition from Schema sheet
            var schemaList = new List<(string Column, string DataType, string Size, bool IsPrimaryKey, bool IsNullable)>();
            int schemaRow = 2;
            while (!string.IsNullOrWhiteSpace(schemaSheet.Cell(schemaRow, 1).GetString()))
            {
                string col = schemaSheet.Cell(schemaRow, 1).GetString().Trim();
                string type = schemaSheet.Cell(schemaRow, 2).GetString().Trim().ToUpper();
                string size = schemaSheet.Cell(schemaRow, 3).GetString().Trim();
                string pk = schemaSheet.Cell(schemaRow, 4).GetString().Trim().ToUpper();
                string nullable = schemaSheet.Cell(schemaRow, 5).GetString().Trim().ToUpper();

                bool isPK = pk == "TRUE" || pk == "YES" || pk == "1";
                bool isNullable = nullable != "FALSE" && nullable != "NO" && nullable != "0";

                schemaList.Add((col, type, size, isPK, isNullable));
                schemaRow++;
            }

            if (schemaList.Count == 0)
            {
                return "No schema definition found starting from row 2 of the Schema sheet.";
            }

            // Validate that all columns in data have schema definitions
            var missingSchemaColumns = columnNames.Except(schemaList.Select(s => s.Column)).ToList();
            if (missingSchemaColumns.Any())
            {
                return $"Schema definitions missing for columns: {string.Join(", ", missingSchemaColumns)}";
            }

            try
            {
                // Get database connection through the connection manager
                using var conn = await _connectionManager.GetConnectionAsync();

                // Check if table already exists
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = $"SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = '{tableName}');";
                var exists = (bool)await checkCmd.ExecuteScalarAsync();
                if (exists)
                    return $"Table '{tableName}' already exists.";

                // Create table SQL
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
                    sb.AppendLine($"    PRIMARY KEY ({string.Join(", ", primaryKeys.Select(pk => $"\"{pk}\""))})");
                else
                    sb.Length -= 3; // Remove last comma and newline

                sb.AppendLine(");");

                // Execute CREATE TABLE
                using var createCmd = conn.CreateCommand();
                createCmd.CommandText = sb.ToString();
                await createCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Created table: {TableName}", tableName);

                // Insert data
                var pkIndexes = primaryKeys.Select(pk => columnNames.IndexOf(pk)).ToList();
                var insertedPKs = new HashSet<string>();
                var errors = new List<string>();
                int rowIdx = 3; // Data starts from row 3
                var insertCount = 0;

                while (!dataSheet.Row(rowIdx).IsEmpty())
                {
                    var values = new List<string>();
                    var pkKey = new StringBuilder();
                    bool hasError = false;

                    for (int i = 0; i < columnNames.Count; i++)
                    {
                        string val = dataSheet.Cell(rowIdx, i + 1).GetString().Trim();
                        var schemaCol = schemaList.FirstOrDefault(c => c.Column == columnNames[i]);

                        if (schemaCol.Column == null)
                        {
                            errors.Add($"Row {rowIdx}: No schema found for column '{columnNames[i]}'.");
                            hasError = true;
                            break;
                        }

                        if (!schemaCol.IsNullable && string.IsNullOrEmpty(val))
                        {
                            errors.Add($"Row {rowIdx}: NULL not allowed in column '{schemaCol.Column}'.");
                            hasError = true;
                        }

                        if (pkIndexes.Contains(i)) pkKey.Append(val).Append("|");
                        if (string.IsNullOrEmpty(val))
                        {
                            values.Add("NULL");
                        }
                        else
                        {
                            if (DateTime.TryParse(val, out var dt))
                            {
                                if (schemaCol.DataType == "DATE")
                                    val = dt.ToString("yyyy-MM-dd");
                                else if (schemaCol.DataType == "TIMESTAMP")
                                    val = dt.ToString("yyyy-MM-dd HH:mm:ss");
                            }

                            values.Add($"'{val.Replace("'", "''")}'");
                        }
                    }

                    if (hasError)
                    {
                        rowIdx++;
                        continue;
                    }

                    // Check for duplicate primary keys
                    string pkComposite = pkKey.ToString();
                    if (!string.IsNullOrEmpty(pkComposite) && !insertedPKs.Add(pkComposite))
                    {
                        errors.Add($"Row {rowIdx}: Duplicate primary key combination.");
                        rowIdx++;
                        continue;
                    }

                    try
                    {
                        var insertSql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames.Select(c => $"\"{c}\""))}) VALUES ({string.Join(", ", values)});";
                        using var insertCmd = conn.CreateCommand();
                        insertCmd.CommandText = insertSql;
                        await insertCmd.ExecuteNonQueryAsync();
                        insertCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowIdx}: DB Insert failed - {ex.Message}");
                        _logger.LogError(ex, "Failed to insert row {RowIndex} into table {TableName}", rowIdx, tableName);
                    }

                    rowIdx++;
                }

                if (workbook.Worksheets.Contains("Report"))
                {
                    var reportSheet = workbook.Worksheet("Report");

                    // Read header
                    var idHeader = reportSheet.Cell(1, 1).GetString().Trim().ToLower();
                    var nameHeader = reportSheet.Cell(1, 2).GetString().Trim().ToLower();
                    var queryHeader = reportSheet.Cell(1, 3).GetString().Trim().ToLower();

                    if (idHeader != "id" || nameHeader != "report name" || queryHeader != "report query")
                    {
                        errors.Add("Report sheet must have headers: id, Report Name, Report Query");
                    }
                    else
                    {
                        int reportRow = 2;
                        while (!reportSheet.Row(reportRow).IsEmpty())
                        {
                            string idStr = reportSheet.Cell(reportRow, 1).GetString().Trim();
                            string reportName = reportSheet.Cell(reportRow, 2).GetString().Trim();
                            string reportQuery = reportSheet.Cell(reportRow, 3).GetString().Trim();

                            if (!int.TryParse(idStr, out var reportId))
                            {
                                errors.Add($"Report Row {reportRow}: Invalid id '{idStr}'");
                                reportRow++;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(reportName) || string.IsNullOrWhiteSpace(reportQuery))
                            {
                                errors.Add($"Report Row {reportRow}: Report Name and Query cannot be empty");
                                reportRow++;
                                continue;
                            }

                            try
                            {
                                var insertReportSql = $@"
                    INSERT INTO report_config (id, report_name, report_query)
                    VALUES (@id, @name, @query);";

                                using var reportCmd = conn.CreateCommand();
                                reportCmd.CommandText = insertReportSql;
                                var paramId = reportCmd.CreateParameter();
                                paramId.ParameterName = "id";
                                paramId.Value = reportId;
                                reportCmd.Parameters.Add(paramId);

                                var paramName = reportCmd.CreateParameter();
                                paramName.ParameterName = "name";
                                paramName.Value = reportName;
                                reportCmd.Parameters.Add(paramName);

                                var paramQuery = reportCmd.CreateParameter();
                                paramQuery.ParameterName = "query";
                                paramQuery.Value = reportQuery;
                                reportCmd.Parameters.Add(paramQuery);

                                await reportCmd.ExecuteNonQueryAsync();
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Report Row {reportRow}: Insert failed - {ex.Message}");
                                _logger.LogError(ex, "Failed to insert report row {ReportRow}", reportRow);
                            }

                            reportRow++;
                        }
                    }
                }

                if (errors.Any())
                {
                    var errorSummary = errors.Count > 10 ?
                        string.Join("\n", errors.Take(10)) + $"\n... and {errors.Count - 10} more errors" :
                        string.Join("\n", errors);
                    return $"Table '{tableName}' created with {insertCount} rows inserted. Some issues occurred:\n{errorSummary}";
                }

                return $"Table '{tableName}' created and {insertCount} row(s) inserted successfully. Report config entries inserted if present.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create table from Excel file");
                return $"Failed to create table: {ex.Message}";
            }
        }
    }
}