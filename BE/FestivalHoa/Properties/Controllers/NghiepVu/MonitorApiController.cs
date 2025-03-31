using Microsoft.AspNetCore.Mvc;
using FestivalHoa.Properties.Models.NghiepVu;
using FestivalHoa.Properties.Exceptions;
using FestivalHoa.Properties.Models.PagingParam;
using FestivalHoa.Properties.FromBodyModels;
using FestivalHoa.Properties.Interfaces.NghiepVu;
using FestivalHoa.Properties.Constants;
using System.Threading.Tasks;
using FestivalHoa.Properties.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace FestivalHoa.Properties.Controllers.NghiepVu
{
    [Route("api/v1/[controller]")]
    [Authorize]
    public class MonitorApiController : ControllerBase
    {
        private readonly IMonitorApiService _monitorApiService;
        private readonly IMonitorApiService _scheduleService;
        private readonly IMonitorApiService _exportCallHistoryToExcel;

        public MonitorApiController(IMonitorApiService monitorApiService, IMonitorApiService scheduleService, IMonitorApiService exportCallHistoryToExcel)
        {
            _monitorApiService = monitorApiService;
            _scheduleService = scheduleService;
            _exportCallHistoryToExcel= exportCallHistoryToExcel;
        }


        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] MonitorApiModel model)
        {
            try
            {
                var data = await _monitorApiService.Create(model);
                return Ok(new ResultMessageResponse()
                    .WithData(data)
                    .WithCode(DefaultCode.SUCCESS)
                    .WithMessage(DefaultMessage.CREATE_SUCCESS));
            }
            catch (ResponseMessageException ex)
            {
                return Ok(new ResultMessageResponse()
                    .WithCode(ex.ResultCode)
                    .WithMessage(ex.ResultString)
                    .WithDetail(ex.Error));
            }
        }

        [HttpPost("create-schedule")]
        
        public async Task<IActionResult> CreateSchedule([FromBody] ScheduleApiCallRequest request)
        {
            try
            {
                await _monitorApiService.ScheduleApiCalls(request);
                return Ok("Lịch gọi API đã được tạo thành công!");
            }
            catch (ResponseMessageException ex)
            {
                return Ok(new ResultMessageResponse()
                    .WithCode(ex.ResultCode)
                    .WithMessage(ex.ResultString)
                    .WithDetail(ex.Error));
            }
        }

        [HttpPost("get-paging-params")]
        public async Task<IActionResult> GetPagingCore([FromBody] PagingParam pagingParam)
        {
            try
            {
                var response = await _monitorApiService.GetPaging(pagingParam);
                return Ok(new ResultMessageResponse()
                    .WithData(response)
                    .WithCode(DefaultCode.SUCCESS)
                    .WithMessage(DefaultMessage.GET_DATA_SUCCESS));
            }
            catch (ResponseMessageException ex)
            {
                return Ok(new ResultMessageResponse()
                    .WithCode(ex.ResultCode)
                    .WithMessage(ex.ResultString));
            }
        }
        [HttpGet("get-all-call-history")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCallHistory()
        {
            try
            {
                List<MonitorApiModel> callHistory = await _monitorApiService.GetAllCallHistory();
                return Ok(new ResultMessageResponse()
                    .WithData(callHistory)
                    .WithCode(DefaultCode.SUCCESS)
                    .WithMessage("Lấy lịch sử call thành công"));
            }
            catch (ResponseMessageException ex)
            {
                return Ok(new ResultMessageResponse()
                    .WithCode(ex.ResultCode)
                    .WithMessage(ex.ResultString)
                    .WithDetail(ex.Error));
            }
        }

        [HttpGet("get-all-schedule")]
        public async Task<IActionResult> GetAllSchedule()
        {
            try
            {
                List<ScheduleApiCallRequest> schedule = await _scheduleService.GetAllSchedule();
                return Ok(new ResultMessageResponse()
                    .WithData(schedule)
                    .WithCode(DefaultCode.SUCCESS)
                    .WithMessage("Lấy lịch call thành công"));
            }
            catch (ResponseMessageException ex)
            {
                return Ok(new ResultMessageResponse()
                    .WithCode(ex.ResultCode)
                    .WithMessage(ex.ResultString)
                    .WithDetail(ex.Error));
            }
        }
        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel()
        {
            try
            {
                var fileBytes = await _monitorApiService.ExportCallHistoryToExcel();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LichSuGoiAPI.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xuất Excel: " + ex.Message });
            }
        }

    }
}
