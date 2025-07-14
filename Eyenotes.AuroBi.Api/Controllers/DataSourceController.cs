using Eyenotes.AuroBi.Application.Services;
using Eyenotes.AuroBi.Domain.Models;
using Eyenotes.AuroBi.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Eyenotes.AuroBi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataSourceController : ControllerBase
    {
        private readonly IDataSourceRepository _repository;
        private readonly IDataSourceService _service;

        public DataSourceController(IDataSourceRepository repository, IDataSourceService service)
        {
            _repository = repository;
            _service = service;
        }

        [HttpPost("connect-sqlserver")]
        public async Task<IActionResult> ConnectToSqlServer([FromBody] SqlConnectionCredentials creds)
        {
            var result = await _repository.SetSqlServerConnectionAsync(creds);
            return Ok(result);
        }

        [HttpPost("connect-postgres")]
        public async Task<IActionResult> ConnectToPostgres([FromBody] PostgresConnectionCredentials creds)
        {
            var result = await _repository.SetPostgresConnectionAsync(creds);
            return Ok(result);
        }

        [HttpPost("upload-excel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadExcel([FromForm] ExcelUploadRequest request)
        {
            var result = await _service.UploadExcelAndCreateTableAsync(request.File);
            return Ok(result);
        }

        [HttpGet("template-excel")]
        public async Task<IActionResult> DownloadExcelTemplate()
        {
            var file = await _service.GenerateExcelTemplateAsync();
            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TableTemplate.xlsx");
        }
    }
}