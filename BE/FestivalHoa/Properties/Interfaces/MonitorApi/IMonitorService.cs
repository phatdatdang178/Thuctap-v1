using FestivalHoa.Properties.Models.Core;
using FestivalHoa.Properties.Models.MonitorApi;

namespace FestivalHoa.Properties.Interfaces.MonitorApi
{
    public interface IMonitorService
    {
        Task<CallHistory> CallApiAsync(MonitorRequestDto request);
        Task<List<CallHistory>> GetCallHistoriesAsync();
        List<CommonModelShort> GetCommonList();
    }
}
