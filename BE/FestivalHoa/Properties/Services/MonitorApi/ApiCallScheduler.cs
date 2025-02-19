using FestivalHoa.Properties.Interfaces.MonitorApi;
using FestivalHoa.Properties.Models.MonitorApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        // Dictionary để theo dõi số lần gọi cho các lịch theo khoảng thời gian, key: schedule.Id, value: (date, count)
        private readonly Dictionary<string, (DateTime date, int count)> _rangeCallTracker = new Dictionary<string, (DateTime, int)>();

        public ApiCallScheduler(IServiceProvider serviceProvider, ILogger<ApiCallScheduler> logger, IHttpClientFactory httpClientFactory)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ApiCallScheduler started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var scheduledCallService = scope.ServiceProvider.GetRequiredService<IScheduledCallService>();
                        var callHistoryService = scope.ServiceProvider.GetRequiredService<IMonitorService>();

                        // Lấy giờ hiện tại theo giờ Việt Nam (UTC+7)
                        var now = DateTime.UtcNow.AddHours(7);
                        var currentTime = now.ToString("HH:mm");
                        _logger.LogInformation($"Current Time: {currentTime}");

                        // Lấy danh sách lịch cấu hình từ DB
                        var schedules = await scheduledCallService.GetScheduledCallsAsync();
                        if (schedules != null && schedules.Any())
                        {
                            foreach (var schedule in schedules)
                            {
                                // Nếu cấu hình theo khoảng thời gian (StartTime, EndTime, CallFrequency)
                                if (!string.IsNullOrEmpty(schedule.StartTime) &&
                                    !string.IsNullOrEmpty(schedule.EndTime) &&
                                    schedule.CallFrequency.HasValue && schedule.CallFrequency.Value > 0)
                                {
                                    if (IsTimeWithinRange(currentTime, schedule.StartTime, schedule.EndTime))
                                    {
                                        // Lấy ngày hôm nay
                                        var today = now.Date;
                                        // Nếu chưa có tracker cho schedule hoặc tracker của ngày khác thì khởi tạo lại
                                        if (!_rangeCallTracker.ContainsKey(schedule.Id) || _rangeCallTracker[schedule.Id].date != today)
                                        {
                                            _rangeCallTracker[schedule.Id] = (today, 0);
                                        }
                                        var tracker = _rangeCallTracker[schedule.Id];

                                        if (tracker.count < schedule.CallFrequency.Value)
                                        {
                                            _logger.LogInformation($"Current time {currentTime} is within range {schedule.StartTime}-{schedule.EndTime} for schedule {schedule.Id}. Triggering range API call ({tracker.count + 1}/{schedule.CallFrequency.Value}).");

                                            // Lấy danh sách API duy nhất từ lịch sử gọi (để tránh gọi trùng lặp)
                                            var callHistories = await callHistoryService.GetCallHistoriesAsync();
                                            var distinctCalls = callHistories
                                                .GroupBy(ch => new { ch.ApiUrl, ch.Method, ch.RequestPayload })
                                                .Select(g => g.First())
                                                .ToList();

                                            foreach (var history in distinctCalls)
                                            {
                                                try
                                                {
                                                    await CallApi(history.ApiUrl, history.Method, history.Name, history.RequestPayload, callHistoryService);
                                                }
                                                catch (Exception ex)
                                                {
                                                    _logger.LogError($"Error calling API {history.ApiUrl}: {ex.Message}");
                                                }
                                            }
                                            // Cập nhật tracker
                                            _rangeCallTracker[schedule.Id] = (today, tracker.count + 1);
                                        }
                                        else
                                        {
                                            _logger.LogInformation($"Schedule {schedule.Id} has already triggered {tracker.count} times today. Skipping further calls in this range.");
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogInformation($"Current time {currentTime} is NOT within range {schedule.StartTime}-{schedule.EndTime} for schedule {schedule.Id}.");
                                    }
                                }
                                else if (schedule.CallTimes != null && schedule.CallTimes.Any())
                                {
                                    // Nếu sử dụng cấu hình gọi cố định theo mảng CallTimes
                                    if (schedule.CallTimes.Contains(currentTime))
                                    {
                                        _logger.LogInformation($"Current time {currentTime} matches fixed schedule (ID: {schedule.Id}). Initiating API calls...");

                                        var callHistories = await callHistoryService.GetCallHistoriesAsync();
                                        var distinctCalls = callHistories
                                            .GroupBy(ch => new { ch.ApiUrl, ch.Method, ch.Name, ch.RequestPayload })
                                            .Select(g => g.First())
                                            .ToList();

                                        foreach (var history in distinctCalls)
                                        {
                                            try
                                            {
                                                await CallApi(history.ApiUrl, history.Method, history.Name, history.RequestPayload, callHistoryService);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError($"Error calling API {history.ApiUrl}: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No scheduled calls found.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ApiCallScheduler encountered an error: {ex.Message}");
                }

                // Kiểm tra lại mỗi 1 phút (có thể điều chỉnh nếu cần)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        // Hàm kiểm tra xem currentTime (định dạng "HH:mm") có nằm trong khoảng [startTime, endTime] hay không
        private bool IsTimeWithinRange(string currentTime, string startTime, string endTime)
        {
            if (TimeSpan.TryParse(currentTime, out var current) &&
                TimeSpan.TryParse(startTime, out var start) &&
                TimeSpan.TryParse(endTime, out var end))
            {
                // Nếu khoảng thời gian vượt qua nửa đêm (không phổ biến)
                if (end < start)
                {
                    return current >= start || current <= end;
                }
                return current >= start && current <= end;
            }
            return false;
        }

        private async Task CallApi(string apiUrl, string method, string name, string payload, IMonitorService callHistoryService)
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
                _logger.LogError($"Exception calling API {apiUrl}: {ex.Message}");
            }

            var callHistory = new CallHistory
            {
                ApiUrl = apiUrl,
                Method = method.ToUpper(),
                Name = name,
                RequestPayload = payload,
                CallTime = DateTime.UtcNow.AddHours(7), // Lưu theo giờ Việt Nam
                Status = isSuccess ? "Success" : "Failure",
                StatusCode = response != null ? (int)response.StatusCode : 500
            };

            await callHistoryService.CreateCallHistoryAsync(callHistory);
            _logger.LogInformation($"Finished calling API: {apiUrl} - Status: {(isSuccess ? "Success" : "Failure")}");
        }
    }
}
