using Microsoft.AspNetCore.Mvc;
using FestivalHoa.Properties.Models.MonitorApi;
using FestivalHoa.Properties.Services.MonitorApi;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using FestivalHoa.Properties.Models.MonitorApi;
using System.Net.NetworkInformation;
using FestivalHoa.Properties.Interfaces.MonitorApi;

namespace FestivalHoa.Properties.Controllers.MonitorApi
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitorController : ControllerBase
    {
        private readonly IMonitorService _callHistoryService;
        private readonly IHttpClientFactory _httpClientFactory;

        public MonitorController(IMonitorService callHistoryService, IHttpClientFactory httpClientFactory)
        {
            _callHistoryService = callHistoryService;
            _httpClientFactory = httpClientFactory;
        }
        [HttpPost("call")]
        public async Task<IActionResult> CallApi([FromBody] MonitorRequestDto request)
        {
            if (string.IsNullOrEmpty(request.ApiUrl) || string.IsNullOrEmpty(request.Method))
            {
                return BadRequest("ApiUrl và Method là bắt buộc.");
            }

            var client = _httpClientFactory.CreateClient();
            HttpResponseMessage response = null;
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
                    return BadRequest("Chỉ hỗ trợ GET và POST.");
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

            // Lấy thời gian UTC và cộng thêm 7 giờ để chuyển thành giờ Việt Nam
            var callTimeVietnam = DateTime.UtcNow.AddHours(7);

            // Lưu lịch sử gọi API
            var callHistory = new CallHistory
            {
                ApiUrl = request.ApiUrl,
                Method = request.Method.ToUpper(),
                Name = request.Name,
                RequestPayload = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : "",
                CallTime = callTimeVietnam, // Lưu trực tiếp giờ Việt Nam
                Note = request.Note,
                Status = isSuccess ? "Success" : "Failure",
                StatusCode = statusCode
            };

            await _callHistoryService.CreateCallHistoryAsync(callHistory);
            // Trả về response chi tiết cho client
            return Ok(new
            {
                ApiUrl = request.ApiUrl,
                Method = request.Method.ToUpper(),
                Name = request.Name,
                StatusCode = statusCode,
                IsSuccess = isSuccess,
                Response = responseContent,
                CallTime = callTimeVietnam
            });
        }

        // Lấy danh sách lịch sử call
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var histories = await _callHistoryService.GetCallHistoriesAsync();
            return Ok(histories);
        }
    }
}
