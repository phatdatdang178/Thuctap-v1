using FestivalHoa.Properties.Models.NghiepVu;
using FestivalHoa.Properties.Models.PagingParam;
using System.Threading.Tasks;

namespace FestivalHoa.Properties.Interfaces.NghiepVu
{
    public interface IMonitorApiService
    {
        Task<dynamic> Create(MonitorApiModel model);
        Task<dynamic> GetPaging(PagingParam pagingParam);
        
    }
}
