using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FestivalHoa.Properties.Exceptions;
using FestivalHoa.Properties.Models.NghiepVu;
using FestivalHoa.Properties.Models.PagingParam;
using FestivalHoa.Properties.Constants;
using FestivalHoa.Properties.Helpers;
using FestivalHoa.Properties.Interfaces.NghiepVu;
using FestivalHoa.Properties.Interfaces.Common;
using FestivalHoa.Properties.FromBodyModels;
using MongoDB.Bson;
using MongoDB.Driver;
using Quartz;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FestivalHoa.Properties.Extensions;
using FestivalHoa.Properties.Installers;
using FestivalHoa.Properties.Models.Core;
using System.Collections.Generic;

namespace FestivalHoa.Properties.Services.NghiepVu
{
    public class MonitorApiService : IMonitorApiService
    {
        // Dùng để cho job truy cập instance hiện tại (nếu MonitorApiService là singleton)
        public static MonitorApiService Instance { get; private set; }

        private readonly DataContext _context;
        private readonly BaseMongoDb<MonitorApiModel, string> _baseMongoDb;
        private readonly ICommonService _commonService;
        private readonly IScheduler _scheduler;
        // Collection lưu lịch gọi API (ví dụ: SCHEDUL)
        private readonly IMongoCollection<ScheduleApiCallRequest> _scheduledCallCollection;

        public MonitorApiService(DataContext context, ICommonService commonService, IScheduler scheduler)
        {
            _context = context;
            _baseMongoDb = new BaseMongoDb<MonitorApiModel, string>(_context.APIDB);
            _commonService = commonService;
            _scheduler = scheduler;
            Instance = this;
            _scheduledCallCollection = _context.SCHEDUL;
        }

        #region CallAndLog: Gọi API và lưu log vào DB

        private async Task<MonitorApiModel> CallAndLog(MonitorApiModel model, bool throwOnFailure)
        {
            HttpResponseMessage response;
            string methodName = "GET";
            if (model.PhuongThuc != null && !string.IsNullOrEmpty(model.PhuongThuc.Name))
                methodName = model.PhuongThuc.Name.ToUpper();

            using (HttpClient client = new HttpClient())
            {
                if (methodName == "POST")
                {
                    string jsonBody = model.BodyParams ?? "";
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    response = await client.PostAsync(model.Url, content);
                }
                else // GET
                {
                    string finalUrl = model.Url;
                    if (!string.IsNullOrEmpty(model.BodyParams))
                    {
                        try
                        {
                            var jObj = JObject.Parse(model.BodyParams);
                            string queryString = ConvertToQueryString(jObj);
                            if (!string.IsNullOrEmpty(queryString))
                            {
                                finalUrl = model.Url.Contains("?")
                                    ? $"{model.Url}&{queryString}"
                                    : $"{model.Url}?{queryString}";
                            }
                        }
                        catch (Exception)
                        {
                            // Nếu không parse được BodyParams, bỏ qua
                        }
                    }
                    response = await client.GetAsync(finalUrl);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                {
                    Code = "TB",
                    CollectionName = "DM_TRANGTHAI"
                });

                var logModel = new MonitorApiModel()
                {
                    Id = BsonObjectId.GenerateNewId().ToString(),
                    Url = model.Url,
                    TrangThai = new CommonModelShort
                    {
                        Id = trangThaiEntity.Id,
                        Code = trangThaiEntity.Code,
                        Name = trangThaiEntity.Name,
                    },
                    Time = DateTime.UtcNow.AddHours(7),
                    CallTimes = model.CallTimes,
                    Name = model.Name,
                    PhuongThuc = model.PhuongThuc,
                    BodyParams = model.BodyParams,
                    GhiChu = $"Call API thất bại với mã: {response.StatusCode}"
                };

                await _baseMongoDb.CreateAsync(logModel);
                if (throwOnFailure)
                {
                    throw new ResponseMessageException()
                        .WithException(DefaultCode.DATA_EXISTED)
                        .WithMessage($"Call API thất bại với mã: {response.StatusCode}");
                }
                return logModel;
            }
            else
            {
                var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                {
                    Code = "TC",
                    CollectionName = "DM_TRANGTHAI"
                });

                var logModel = new MonitorApiModel()
                {
                    Id = BsonObjectId.GenerateNewId().ToString(),
                    Url = model.Url,
                    TrangThai = new CommonModelShort
                    {
                        Id = trangThaiEntity.Id,
                        Code = trangThaiEntity.Code,
                        Name = trangThaiEntity.Name,
                    },
                    Time = DateTime.UtcNow.AddHours(7),
                    CallTimes = model.CallTimes,
                    Name = model.Name,
                    PhuongThuc = model.PhuongThuc,
                    BodyParams = model.BodyParams,
                    GhiChu = $"Call API thành công với mã: {response.StatusCode}"
                };

                var result = await _baseMongoDb.CreateAsync(logModel);
                if (result.Entity.Id == default || !result.Success)
                {
                    if (throwOnFailure)
                        throw new ResponseMessageException().WithException(DefaultCode.CREATE_FAILURE);
                }
                return logModel;
            }
        }

        #endregion

        #region Create (gọi API ngay lập tức)

        public async Task<dynamic> Create(MonitorApiModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Url))
                    throw new ResponseMessageException().WithException(DefaultCode.ERROR_STRUCTURE);

                if (model.PhuongThuc != null)
                {
                    if (!ObjectId.TryParse(model.PhuongThuc.Id, out _))
                    {
                        throw new ResponseMessageException()
                            .WithException(DefaultCode.ERROR_STRUCTURE)
                            .WithMessage("Trường _id của PhuongThuc không hợp lệ.");
                    }
                }

                return await CallAndLog(model, true);
            }
            catch (ResponseMessageException e)
            {
                throw new ResponseMessageException()
                    .WithCode(DefaultCode.EXCEPTION)
                    .WithMessage(e.ResultString)
                    .WithDetail(e.Error);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("is not a valid 24 digit hex string."))
                    throw new ResponseMessageException().WithException(DefaultCode.ID_NOT_CORRECT_FORMAT);
                throw new ResponseMessageException().WithCode(DefaultCode.EXCEPTION).WithMessage(ex.Message);
            }
        }

        #endregion

        #region Schedule API Calls (lên lịch gọi API)

        public async Task<dynamic> ScheduleApiCalls(ScheduleApiCallRequest request)
        {
            // Lặp qua từng lịch set (theo danh sách giờ cụ thể hoặc theo khoảng thời gian)
            if (request.SpecificTimes != null && request.SpecificTimes.Any())
            {
                foreach (var timeStr in request.SpecificTimes)
                {
                    if (TimeSpan.TryParse(timeStr, out TimeSpan parsedTime))
                    {
                        DateTime now = DateTime.Now;
                        DateTime scheduledTime = new DateTime(now.Year, now.Month, now.Day,
                            parsedTime.Hours, parsedTime.Minutes, 0);
                        if (scheduledTime < now)
                            scheduledTime = scheduledTime.AddDays(1);
                        await ScheduleJobAt(request.MonitorApiModel, scheduledTime);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(request.StartTime) &&
                     !string.IsNullOrEmpty(request.EndTime) &&
                     request.CallFrequency.HasValue && request.CallFrequency.Value > 0)
            {
                if (TimeSpan.TryParse(request.StartTime, out TimeSpan start) &&
                    TimeSpan.TryParse(request.EndTime, out TimeSpan end))
                {
                    DateTime now = DateTime.Now;
                    DateTime startDateTime = new DateTime(now.Year, now.Month, now.Day,
                        start.Hours, start.Minutes, 0);
                    DateTime endDateTime = new DateTime(now.Year, now.Month, now.Day,
                        end.Hours, end.Minutes, 0);
                    if (endDateTime <= startDateTime)
                        endDateTime = endDateTime.AddDays(1);

                    int frequency = request.CallFrequency.Value;
                    if (frequency == 1)
                    {
                        if (startDateTime < now)
                            startDateTime = startDateTime.AddDays(1);
                        await ScheduleJobAt(request.MonitorApiModel, startDateTime);
                    }
                    else
                    {
                        TimeSpan interval = TimeSpan.FromTicks((endDateTime - startDateTime).Ticks / (frequency - 1));
                        for (int i = 0; i < frequency; i++)
                        {
                            DateTime scheduledTime = startDateTime.AddTicks(interval.Ticks * i);
                            if (scheduledTime < now)
                                scheduledTime = scheduledTime.AddDays(1);
                            await ScheduleJobAt(request.MonitorApiModel, scheduledTime);
                        }
                    }
                }
            }
            return new { Message = "Đã lên lịch call API thành công" };
        }

        // Lên lịch job tại thời điểm xác định và lưu lịch vào DB (dành cho quản lý lịch)
        public async Task ScheduleJobAt(MonitorApiModel monitorApiModel, DateTime scheduledTime)
        {
            // Serialize đối tượng MonitorApiModel để truyền qua JobDataMap
            string monitorApiModelJson = JsonConvert.SerializeObject(monitorApiModel);

            IJobDetail job = JobBuilder.Create<ApiCallJob>()
                .WithIdentity(Guid.NewGuid().ToString())
                .UsingJobData("MonitorApiModel", monitorApiModelJson)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .StartAt(scheduledTime)
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                .Build();

            await _scheduler.ScheduleJob(job, trigger);
            Console.WriteLine($"Đã lên lịch call API tại: {scheduledTime}");

            // Lưu lịch gọi API vào DB để quản lý
            var scheduleRecord = new ScheduleApiCallRequest
            {
                MonitorApiModel = monitorApiModel,
                SpecificTimes = new List<string> { scheduledTime.ToString("HH:mm") },
                // có thể lưu thêm thông tin như StartTime, EndTime, CallFrequency nếu cần
            };
            await _scheduledCallCollection.InsertOneAsync(scheduleRecord);
        }

        #endregion

        #region GetPaging (phân trang)

        public async Task<dynamic> GetPaging(PagingParam pagingParam)
        {
            PagingModel<dynamic> result = new PagingModel<dynamic>();
            var builder = Builders<MonitorApiModel>.Filter;
            var filter = builder.Empty;
            filter = builder.And(filter, builder.Eq("IsDeleted", false));
            if (!string.IsNullOrEmpty(pagingParam.TrangThaiCode))
            {
                filter = builder.And(filter,
                    builder.Eq("TrangThai.Code", pagingParam.TrangThaiCode)
                );
            }

            result.TotalRows = await _context.APIDB.CountDocumentsAsync(filter);

            string sortBy = !string.IsNullOrEmpty(pagingParam.SortBy)
                ? FormatterString.HandlerSortBy(pagingParam.SortBy)
                : "CreatedAt";
            result.Data = await _context.APIDB.Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .ThenByDescending(e => e.CreatedAt)
                .Skip(pagingParam.Skip)
                .Limit(pagingParam.Limit)
                .ToListAsync();

            return result;
        }

        #endregion

        #region Helper

        private string ConvertToQueryString(JObject jObj)
        {
            var list = new List<string>();
            foreach (var prop in jObj.Properties())
                list.Add($"{prop.Name}={Uri.EscapeDataString(prop.Value.ToString())}");
            return string.Join("&", list);
        }

        #endregion

        #region Nested Job Class

        public class ApiCallJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                var dataMap = context.JobDetail.JobDataMap;
                string monitorApiModelJson = dataMap.GetString("MonitorApiModel");
                if (string.IsNullOrEmpty(monitorApiModelJson))
                    throw new ArgumentException("MonitorApiModel không tồn tại trong JobDataMap.");

                MonitorApiModel monitorApiModel = JsonConvert.DeserializeObject<MonitorApiModel>(monitorApiModelJson);
                // Gọi API và lưu log vào DB (không ném exception nếu thất bại)
                await MonitorApiService.Instance.CallAndLog(monitorApiModel, throwOnFailure: false);
            }
        }

        #endregion

        #region Implement IJobExecution (Interface Method)

        public async Task Execute(IJobExecutionContext context)
        {
            var job = new ApiCallJob();
            await job.Execute(context);
        }

        #endregion
    }
}
