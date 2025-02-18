using FestivalHoa.Properties.Interfaces.MonitorApi;
using FestivalHoa.Properties.Models.MonitorApi;
using Microsoft.AspNetCore.Mvc;

namespace FestivalHoa.Properties.Controllers.MonitorApi
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitorController : ControllerBase
    {
        private readonly IMonitorService _monitorService;

        public MonitorController(IMonitorService monitorService)
        {
            _monitorService = monitorService;
        }

        [HttpPost("call")]
        public async Task<IActionResult> CallApi([FromBody] MonitorRequestDto request)
        {
            if (string.IsNullOrEmpty(request.ApiUrl) || string.IsNullOrEmpty(request.Method))
                return BadRequest("ApiUrl và Method là bắt buộc.");

            var result = await _monitorService.CallApiAsync(request);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var histories = await _monitorService.GetCallHistoriesAsync();
            return Ok(histories);
        }

        [HttpGet("common")]
        public IActionResult GetCommonList()
        {
            return Ok(_monitorService.GetCommonList());
        }
    }
}
