using Eyenotes.AuroBi.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Eyenotes.AuroBi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IMetaDataService _metaDataService;

        public HealthController(IMetaDataService metaDataService)
        {
            _metaDataService = metaDataService;
        }

        [HttpGet("connection")]
        public async Task<IActionResult> CheckConnection()
        {
            try
            {
                var isHealthy = await _metaDataService.TestConnectionAsync();
                return Ok(new { isHealthy, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return Ok(new { isHealthy = false, error = ex.Message, timestamp = DateTime.UtcNow });
            }
        }
    }
}
