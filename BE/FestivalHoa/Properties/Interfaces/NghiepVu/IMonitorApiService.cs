using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        Task ScheduleJobAt(ScheduleApiCallRequest request, TimeSpan scheduledTime);

        Task Execute(IJobExecutionContext context);
        Task<List<MonitorApiModel>> GetAllCallHistory();
        Task<List<ScheduleApiCallRequest>> GetAllSchedule();
        Task ResumeScheduledCalls();
        Task<byte[]> ExportCallHistoryToExcel();
    }
}
