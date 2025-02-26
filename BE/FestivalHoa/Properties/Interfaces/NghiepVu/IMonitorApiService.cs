using System;
using System.Threading.Tasks;
using FestivalHoa.Properties.Models.NghiepVu;
using FestivalHoa.Properties.Models.PagingParam;
using Quartz;

namespace FestivalHoa.Properties.Interfaces.NghiepVu
{
    public interface IMonitorApiService
    {
        Task<dynamic> Create(MonitorApiModel model);
        Task<dynamic> GetPaging(PagingParam pagingParam);
        Task<dynamic> ScheduleApiCalls(ScheduleApiCallRequest model);
        Task ScheduleJobAt(MonitorApiModel monitorApiModel, DateTime scheduledTime);
        Task Execute(IJobExecutionContext context);
    }
}
