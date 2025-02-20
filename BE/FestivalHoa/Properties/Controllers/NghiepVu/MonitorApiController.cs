using Microsoft.AspNetCore.Mvc;
using FestivalHoa.Properties.Constants;
using FestivalHoa.Properties.Installers;
using FestivalHoa.Properties.Interfaces.NghiepVu;
using FestivalHoa.Properties.Controllers.Core;
using FestivalHoa.Properties.Models.CongDan;
using FestivalHoa.Properties.Exceptions;
using FestivalHoa.Properties.Helpers;
using FestivalHoa.Properties.Models.PagingParam;
using FestivalHoa.Properties.FromBodyModels;
using FestivalHoa.Properties.Models.NghiepVu;
using FestivalHoa.Properties.Models.NghiepVu;

namespace FestivalHoa.Properties.Controllers.NghiepVu
{
    [Route("api/v1/[controller]")]
    public class MonitorApiController : DefaultReposityController<MonitorApiModel>
    {
        private readonly IMonitorApiService _service;
        private DataContext _dataContext;
        private static string NameCollection = DefaultNameCollection.LOGCALLAPI;
        public MonitorApiController(DataContext context, IMonitorApiService service) : base(context, NameCollection)
        {
            _service = service;
            _dataContext = context;
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] MonitorApiModel model)
        {
            try
            {

                var data = await _service.Create(model);
                return Ok(
                    new ResultMessageResponse()
                        .WithData(data)
                        .WithCode(DefaultCode.SUCCESS)
                        .WithMessage(DefaultMessage.CREATE_SUCCESS)
                );
            }
            catch (ResponseMessageException ex)
            {
                return Ok(
                    new ResultMessageResponse().WithCode(ex.ResultCode)
                        .WithMessage(ex.ResultString).WithDetail(ex.Error)
                );
            }
        }

        [HttpPost]
        [Route("autoCall")]
        public async Task<IActionResult> AutoCall([FromBody] MonitorApiModel model)
        {
            try
            {
                var data = await _service.AutoCall(model);
                return Ok(
                    new ResultMessageResponse()
                        .WithData(data)
                        .WithCode(DefaultCode.SUCCESS)
                        .WithMessage("Auto call executed successfully")
                );
            }
            catch (ResponseMessageException ex)
            {
                return Ok(
                    new ResultMessageResponse()
                        .WithCode(ex.ResultCode)
                        .WithMessage(ex.ResultString)
                        .WithDetail(ex.Error)
                );
            }
        }


        //[HttpPost]
        //[Route("get-paging-params")]
        //public virtual async Task<IActionResult> GetPagingCore([FromBody] PagingParam pagingParam)
        //{
        //    try
        //    {
        //        var response = await _service.GetPaging(pagingParam);
        //        return Ok(
        //            new ResultMessageResponse()
        //                .WithData(response)
        //                .WithCode(DefaultCode.SUCCESS)
        //                .WithMessage(DefaultMessage.GET_DATA_SUCCESS)
        //        );
        //    }
        //    catch (ResponseMessageException ex)
        //    {
        //        return Ok(
        //            new ResultMessageResponse().WithCode(ex.ResultCode)
        //                .WithMessage(ex.ResultString)
        //        );
        //    }
        //}



    }
}
