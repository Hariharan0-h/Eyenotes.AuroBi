using Eyenotes.AuroBi.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Eyenotes.AuroBi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetaDataController : ControllerBase
    {
        private readonly IMetaDataService _metaDataService;

        public MetaDataController(IMetaDataService metaDataService)
        {
            _metaDataService = metaDataService;
        }

        [HttpGet("tables")]
        public async Task<IActionResult> GetTableNames()
        {
            var tables = await _metaDataService.GetAllTableNamesAsync();
            return Ok(tables);
        }

        [HttpGet("columns/{tableName}")]
        public async Task<IActionResult> GetTableColumns(string tableName)
        {
            var columns = await _metaDataService.GetTableColumnsAsync(tableName);
            return Ok(columns);
        }

        [HttpGet("data/{tableName}")]
        public async Task<IActionResult> GetTableData(string tableName)
        {
            var data = await _metaDataService.GetTableDataAsync(tableName);
            return Ok(data);
        }

        [HttpPost("run-query")]
        public async Task<IActionResult> RunQuery([FromBody] string query)
        {
            if (!query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only SELECT queries are allowed.");

            var result = await _metaDataService.RunQueryAsync(query);
            return Ok(result);
        }
    }
}
