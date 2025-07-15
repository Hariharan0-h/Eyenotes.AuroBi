using Microsoft.AspNetCore.Mvc;
using Eyenotes.AuroBi.Application.Services;

namespace Eyenotes.AuroBi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsService _service;

        public ReportsController(IReportsService service)
        {
            _service = service;
        }

        [HttpGet("get-report-configurations")]
        public async Task<IActionResult> GetReportConfigurations()
        {
            try
            {
                var result = await _service.GetReportsConfig();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpPost("get-report-data")]
        public async Task<IActionResult> GetReportData([FromBody] string query)
        {
            try
            {
                var result = await _service.GetReportData(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }
}
