using FestivalHoa.Properties.Models.MonitorApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalHoa.Properties.Interfaces.MonitorApi
{
    public interface IScheduledCallService
    {
        Task<List<ScheduledCall>> GetScheduledCallsAsync();
        Task CreateOrUpdateScheduleAsync(ScheduledCall schedule);
    }
}
