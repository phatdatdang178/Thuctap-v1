using FestivalHoa.Properties.Constants;
using FestivalHoa.Properties.Interfaces.MonitorApi;
using FestivalHoa.Properties.Models.Core;
using FestivalHoa.Properties.Models.MonitorApi;
using MongoDB.Driver;
using System.Text;
using System.Text.Json;

namespace FestivalHoa.Properties.Services.MonitorApi
{
    public class CallHistoryService : IMonitorService
    {
        private readonly IMongoCollection<CallHistory> _callHistories;

        public CallHistoryService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetValue<string>("DbSettings:ConnectionString"));
            var database = client.GetDatabase(configuration.GetValue<string>("DbSettings:DatabaseName"));
            _callHistories = database.GetCollection<CallHistory>("CallHistories");
        }

        public async Task<List<CallHistory>> GetCallHistoriesAsync()
        {
            // Lấy tất cả lịch sử call, có thể sắp xếp sao cho các call thất bại được ưu tiên (ở đây sắp xếp theo trường Status)
            var sort = Builders<CallHistory>.Sort.Ascending(x => x.Status);
            return await _callHistories.Find(_ => true).Sort(sort).ToListAsync();
        }

        public async Task<List<CallHistory>> GetFailedCallsAsync()
        {
            return await _callHistories
                .Find(x => x.Status == "Failure")
                .SortByDescending(x => x.CallTime)
                .ToListAsync();
        }

        public async Task CreateCallHistoryAsync(CallHistory history)
        {
            await _callHistories.InsertOneAsync(history);
        }
    }
}
