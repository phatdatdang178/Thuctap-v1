using FestivalHoa.Properties.FromBodyModels;
using FestivalHoa.Properties.Models.CongDan;
using FestivalHoa.Properties.Models.NghiepVu;
using FestivalHoa.Properties.Models.PagingParam;

namespace FestivalHoa.Properties.Interfaces.NghiepVu
{

    public interface IMonitorApiService
    {
        Task<dynamic> Create(MonitorApiModel model);
        Task<dynamic> AutoCall(MonitorApiModel model);

    }
}

