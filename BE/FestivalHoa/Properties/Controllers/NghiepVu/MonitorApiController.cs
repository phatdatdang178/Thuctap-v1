using Microsoft.AspNetCore.Mvc;
using FestivalHoa.Properties.Models.NghiepVu;
using FestivalHoa.Properties.Exceptions;
using FestivalHoa.Properties.Models.PagingParam;
using FestivalHoa.Properties.FromBodyModels;
using FestivalHoa.Properties.Interfaces.NghiepVu;
using FestivalHoa.Properties.Constants;
using System.Threading.Tasks;
using FestivalHoa.Properties.Helpers;

namespace FestivalHoa.Properties.Controllers.NghiepVu
{
    [Route("api/v1/[controller]")]
    public class MonitorApiController : ControllerBase
    {
        private readonly IMonitorApiService _monitorApiService;

        public MonitorApiController(IMonitorApiService monitorApiService)
        {
            _monitorApiService = monitorApiService;
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
    }
}
