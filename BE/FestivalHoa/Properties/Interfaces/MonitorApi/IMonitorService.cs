using FestivalHoa.Properties.Models.Core;
using FestivalHoa.Properties.Models.MonitorApi;

namespace FestivalHoa.Properties.Interfaces.MonitorApi
{
    public interface IMonitorService
    {
        Task CreateCallHistoryAsync(CallHistory history);
        Task<List<CallHistory>> GetCallHistoriesAsync();
        Task<List<CallHistory>> GetFailedCallsAsync();
    }
}
