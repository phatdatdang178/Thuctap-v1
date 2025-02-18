using FestivalHoa.Properties.Constants;
using FestivalHoa.Properties.Interfaces.MonitorApi;
using FestivalHoa.Properties.Models.Core;
using FestivalHoa.Properties.Models.MonitorApi;
using MongoDB.Driver;
using System.Text;
using System.Text.Json;

namespace FestivalHoa.Properties.Services.MonitorApi
{
    public class MonitorService : IMonitorService
    {
        private readonly IMongoCollection<CallHistory> _callHistoryCollection;
        private readonly IHttpClientFactory _httpClientFactory;

        public MonitorService(IMongoClient mongoClient, IHttpClientFactory httpClientFactory)
        {
            var database = mongoClient.GetDatabase("MonitorDb");
            _callHistoryCollection = database.GetCollection<CallHistory>("CallHistories");
            _httpClientFactory = httpClientFactory;
        }

        public async Task<CallHistory> CallApiAsync(MonitorRequestDto request)
        {
            var client = _httpClientFactory.CreateClient();
            HttpResponseMessage response;
            string responseContent = string.Empty;
            int statusCode = 0;
            bool isSuccess = false;

            try
            {
                if (request.Method.ToUpper() == "GET")
                {
                    response = await client.GetAsync(request.ApiUrl);
                }
                else if (request.Method.ToUpper() == "POST")
                {
                    string jsonPayload = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : "";
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    response = await client.PostAsync(request.ApiUrl, content);
                }
                else
                {
                    throw new Exception("Chỉ hỗ trợ GET và POST.");
                }

                statusCode = (int)response.StatusCode;
                responseContent = await response.Content.ReadAsStringAsync();
                isSuccess = response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                responseContent = ex.Message;
                statusCode = 500;
                isSuccess = false;
            }

            var callHistory = new CallHistory
            {
                ApiUrl = request.ApiUrl,
                Method = request.Method.ToUpper(),
                RequestPayload = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : "",
                CallTime = DateTime.UtcNow.AddHours(7),
                Note = request.Note,
                Status = isSuccess ? "Success" : "Failure",
                StatusCode = statusCode
            };

            await _callHistoryCollection.InsertOneAsync(callHistory);
            return callHistory;
        }

        public async Task<List<CallHistory>> GetCallHistoriesAsync()
        {
            return await _callHistoryCollection.Find(_ => true).ToListAsync();
        }

        public List<CommonModelShort> GetCommonList()
        {
            return ListCommon.listCommon;
        }
    }
}
