using FestivalHoa.Properties.Interfaces.MonitorApi;
using FestivalHoa.Properties.Models.MonitorApi;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FestivalHoa.Properties.Services.MonitorApi
{
    public class ScheduledCallService : IScheduledCallService
    {
        private readonly IMongoCollection<ScheduledCall> _scheduledCalls;

        public ScheduledCallService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetValue<string>("DbSettings:ConnectionString"));
            var database = client.GetDatabase(configuration.GetValue<string>("DbSettings:DatabaseName"));
            _scheduledCalls = database.GetCollection<ScheduledCall>("ScheduledCalls");
        }

        public async Task<List<ScheduledCall>> GetScheduledCallsAsync()
        {
            return await _scheduledCalls.Find(x => x.IsActive).ToListAsync();
        }

        public async Task CreateOrUpdateScheduleAsync(ScheduledCall schedule)
        {
            var existing = await _scheduledCalls.Find(x => x.Id == schedule.Id).FirstOrDefaultAsync();
            if (existing == null)
            {
                await _scheduledCalls.InsertOneAsync(schedule);
            }
            else
            {
                var filter = Builders<ScheduledCall>.Filter.Eq(x => x.Id, schedule.Id);
                await _scheduledCalls.ReplaceOneAsync(filter, schedule);
            }
        }
    }
}
