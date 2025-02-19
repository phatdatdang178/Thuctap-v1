using FestivalHoa.Properties.Interfaces.MonitorApi;
using FestivalHoa.Properties.Models.MonitorApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FestivalHoa.Properties.Services.MonitorApi
{
    public class ApiCallScheduler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApiCallScheduler> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiCallScheduler(IServiceProvider serviceProvider, ILogger<ApiCallScheduler> logger, IHttpClientFactory httpClientFactory)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scheduledCallService = scope.ServiceProvider.GetRequiredService<IScheduledCallService>();
                    var callHistoryService = scope.ServiceProvider.GetRequiredService<IMonitorService>();

                    var now = DateTime.UtcNow.AddHours(7); // Giờ Việt Nam
                    var currentTime = now.ToString("HH:mm");

                    _logger.LogInformation($"Current Time: {currentTime}"); // In ra thời gian hiện tại để kiểm tra

                    // Lấy danh sách lịch đã cấu hình
                    var schedules = await scheduledCallService.GetScheduledCallsAsync();

                    foreach (var schedule in schedules)
                    {
                        // Đảm bảo so sánh thời gian đúng định dạng
                        foreach (var callTime in schedule.CallTimes)
                        {
                            _logger.LogInformation($"Checking if {currentTime} matches schedule time: {callTime}"); // Log thời gian gọi API trong lịch

                            if (currentTime == callTime)
                            {
                                _logger.LogInformation($"Thời gian {currentTime} - Gọi lại API từ lịch sử...");

                                var callHistories = await callHistoryService.GetCallHistoriesAsync();

                                foreach (var history in callHistories)
                                {
                                    await CallApi(history.ApiUrl, history.Method, history.RequestPayload, callHistoryService);
                                }
                            }
                        }
                    }
                }

                // Đảm bảo mỗi 1 phút kiểm tra lại
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CallApi(string apiUrl, string method, string payload, IMonitorService callHistoryService)
        {
            var client = _httpClientFactory.CreateClient();
            HttpResponseMessage response = null;
            bool isSuccess = false;
            string responseContent = "";

            try
            {
                _logger.LogInformation($"Calling API: {apiUrl} with method {method}");

                if (method.ToUpper() == "GET")
                {
                    response = await client.GetAsync(apiUrl);
                }
                else if (method.ToUpper() == "POST")
                {
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    response = await client.PostAsync(apiUrl, content);
                }

                responseContent = await response.Content.ReadAsStringAsync();
                isSuccess = response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                responseContent = ex.Message;
                isSuccess = false;
            }

            var callHistory = new CallHistory
            {
                ApiUrl = apiUrl,
                Method = method.ToUpper(),
                RequestPayload = payload,
                CallTime = DateTime.UtcNow.AddHours(7), // Lưu theo giờ VN
                Status = isSuccess ? "Success" : "Failure",
                StatusCode = response != null ? (int)response.StatusCode : 500
            };

            await callHistoryService.CreateCallHistoryAsync(callHistory);
        }
    }

}
