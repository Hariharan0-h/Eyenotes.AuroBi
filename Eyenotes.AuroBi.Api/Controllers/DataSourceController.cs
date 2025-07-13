using Eyenotes.AuroBi.Domain.Models;
using Eyenotes.AuroBi.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Eyenotes.AuroBi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataSourceController : ControllerBase
    {
        private readonly IDataSourceRepository _repository;

        public DataSourceController(IDataSourceRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("connect-sqlserver")]
        public async Task<IActionResult> ConnectToSqlServer([FromBody] SqlConnectionCredentials creds)
        {
            var result = await _repository.GetSqlServerConnectionAsync(creds);
            return Ok(result);
        }

        [HttpPost("connect-postgres")]
        public async Task<IActionResult> ConnectToPostgres([FromBody] PostgresConnectionCredentials creds)
        {
            var result = await _repository.GetPostgresConnectionAsync(creds);
            return Ok(result);
        }

        [HttpPost("upload-excel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadExcel([FromForm] ExcelUploadRequest request)
        {
            var result = await _repository.UploadExcelAndCreateTableAsync(request.File);
            return Ok(result);
        }

        [HttpGet("template-excel")]
        public async Task<IActionResult> DownloadExcelTemplate()
        {
            var file = await _repository.GenerateExcelTemplateAsync();
            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TableTemplate.xlsx");
        }
    }
}
