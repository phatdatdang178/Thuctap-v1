using FestivalHoa.Properties.Interfaces.MonitorApi;
using FestivalHoa.Properties.Models.MonitorApi;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalHoa.Properties.Controllers.MonitorApi
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduledCallService _scheduledCallService;

        public ScheduleController(IScheduledCallService scheduledCallService)
        {
            _scheduledCallService = scheduledCallService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSchedules()
        {
            var schedules = await _scheduledCallService.GetScheduledCallsAsync();
            return Ok(schedules);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateSchedule([FromBody] ScheduledCall schedule)
        {
            await _scheduledCallService.CreateOrUpdateScheduleAsync(schedule);
            return Ok(new { message = "Lịch gọi API đã được cập nhật" });
        }
    }
}
