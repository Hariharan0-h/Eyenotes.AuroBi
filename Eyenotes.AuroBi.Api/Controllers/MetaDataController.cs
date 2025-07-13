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
    }
}
