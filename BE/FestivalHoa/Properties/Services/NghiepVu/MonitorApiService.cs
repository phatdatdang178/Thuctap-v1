using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
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
using System.IO;
using ClosedXML.Excel;

namespace FestivalHoa.Properties.Services.NghiepVu
{
    public class MonitorApiService :    IMonitorApiService
    {
        // Cho phép các job truy cập instance hiện tại (nếu sử dụng singleton)
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

            // Xác định phương thức gọi API (GET/POST)
            string methodName = model.PhuongThuc?.Name?.Trim().ToUpper() ?? "GET";

            using (HttpClient client = new HttpClient())
            {
                if (methodName == "POST")
                {
                    string jsonBody = model.BodyParams ?? "";
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    response = await client.PostAsync(model.Url, content);
                }
                else // Mặc định là GET
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
                            // Bỏ qua nếu không parse được BodyParams
                        }
                    }
                    response = await client.GetAsync(finalUrl);
                }
            }

            int statusCode = (int)response.StatusCode;

            // Lấy trạng thái tương ứng (thất bại hoặc thành công)
            string trangThaiCode = response.IsSuccessStatusCode ? "TC" : "TB";
            var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
            {
                Code = trangThaiCode,
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
                Name = model.Name,
                PhuongThuc = model.PhuongThuc, // Lưu phương thức dựa trên Name
                BodyParams = model.BodyParams,
                GhiChu = model.GhiChu,
                Code = $"{statusCode}"
            };

            var result = await _baseMongoDb.CreateAsync(logModel);

            if (!response.IsSuccessStatusCode || result.Entity.Id == default || !result.Success)
            {
                if (throwOnFailure)
                {
                    throw new ResponseMessageException()
                        .WithException(DefaultCode.CREATE_FAILURE)
                        .WithMessage($"Call API thất bại với mã: {statusCode}");
                }
            }

            return logModel;
        }


        #endregion

        #region Create (gọi API ngay lập tức)

        public async Task<dynamic> Create(MonitorApiModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Url))
                    throw new ResponseMessageException().WithException(DefaultCode.ERROR_STRUCTURE);

                if (model.PhuongThuc == null || string.IsNullOrEmpty(model.PhuongThuc.Name))
                {
                    throw new ResponseMessageException()
                        .WithException(DefaultCode.ERROR_STRUCTURE)
                        .WithMessage("PhuongThuc.Name không được để trống.");
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

        // Hỗ trợ độc lập hai loại cấu hình: specificTimes và range (StartTime, EndTime, CallFrequency)
        public async Task<dynamic> ScheduleApiCalls(ScheduleApiCallRequest request)
        {
            // Xử lý cấu hình theo specificTimes (danh sách giờ cụ thể)
            if (request.SpecificTimes != null && request.SpecificTimes.Any())
            {
                foreach (var timeStr in request.SpecificTimes)
                {
                    if (TimeSpan.TryParse(timeStr, out TimeSpan parsedTime))
                    {
                        await ScheduleJobAt(request, parsedTime);
                    }
                }
            }

            // Xử lý cấu hình theo khoảng thời gian (StartTime, EndTime, CallFrequency)
            if (!string.IsNullOrEmpty(request.StartTime) &&
                !string.IsNullOrEmpty(request.EndTime) &&
                request.CallFrequency.HasValue && request.CallFrequency.Value > 0)
            {
                if (TimeSpan.TryParse(request.StartTime, out TimeSpan start) &&
                    TimeSpan.TryParse(request.EndTime, out TimeSpan end))
                {
                    int frequency = request.CallFrequency.Value;
                    if (frequency == 1)
                    {
                        await ScheduleJobAt(request, start);
                    }
                    else
                    {
                        // Tính khoảng cách đều nhau giữa các lần gọi: (end - start) / (frequency - 1)
                        TimeSpan interval = TimeSpan.FromTicks((end - start).Ticks / (frequency - 1));
                        for (int i = 0; i < frequency; i++)
                        {
                            TimeSpan scheduledTime = start.Add(TimeSpan.FromTicks(interval.Ticks * i));
                            await ScheduleJobAt(request, scheduledTime);
                        }
                    }
                }
            }
            return new { Message = "Đã lên lịch call API thành công" };
        }

        // Lên lịch job theo một TimeSpan (giờ:phút) với Cron trigger hàng ngày.
        // Mỗi job được lên lịch hàng ngày dựa trên giờ và phút đã set.
        public async Task ScheduleJobAt(ScheduleApiCallRequest request, TimeSpan scheduledTime)
        {
            // Tạo Cron expression: "0 {minute} {hour} * * ?" (chạy hàng ngày vào giờ và phút đó)
            string cronExpression = $"0 {scheduledTime.Minutes} {scheduledTime.Hours} * * ?";
            IJobDetail job = JobBuilder.Create<ApiCallJob>()
                .WithIdentity(Guid.NewGuid().ToString())
                .UsingJobData("MonitorApiModel", JsonConvert.SerializeObject(request.MonitorApiModel))
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithCronSchedule(cronExpression, x => x.WithMisfireHandlingInstructionFireAndProceed())
                .Build();

            await _scheduler.ScheduleJob(job, trigger);
            Console.WriteLine($"Đã lên lịch call API hàng ngày lúc: {scheduledTime:hh\\:mm}");

            // Cập nhật (hoặc tạo mới) record lịch trong DB cho đầu API này.
            var filter = Builders<ScheduleApiCallRequest>.Filter.Eq(s => s.MonitorApiModel.Url, request.MonitorApiModel.Url);
            var existingRecord = await _scheduledCallCollection.Find(filter).FirstOrDefaultAsync();
            string scheduledTimeStr = scheduledTime.ToString(@"hh\:mm");
            if (existingRecord != null)
            {
                if (existingRecord.SpecificTimes == null)
                    existingRecord.SpecificTimes = new List<string>();
                // Nếu chưa có giờ này trong danh sách, thêm vào
                if (!existingRecord.SpecificTimes.Contains(scheduledTimeStr))
                {
                    existingRecord.SpecificTimes.Add(scheduledTimeStr);
                    // Cập nhật lại các thông tin khác nếu cần (StartTime, EndTime, CallFrequency)
                    existingRecord.StartTime = request.StartTime;
                    existingRecord.EndTime = request.EndTime;
                    existingRecord.CallFrequency = request.CallFrequency;
                    await _scheduledCallCollection.ReplaceOneAsync(filter, existingRecord);
                }
            }
            else
            {
                var scheduleRecord = new ScheduleApiCallRequest
                {
                    MonitorApiModel = request.MonitorApiModel,
                    SpecificTimes = new List<string> { scheduledTimeStr },
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    CallFrequency = request.CallFrequency
                };
                await _scheduledCallCollection.InsertOneAsync(scheduleRecord);
            }
        }

        #endregion

        #region Resume Scheduled Calls (Re-schedule khi ứng dụng khởi động lại)

        /// <summary>
        /// Phương thức này được gọi khi ứng dụng khởi động lại để re-schedule các job Quartz
        /// dựa trên các record lịch trong DB. Nếu thời gian trong record (theo SpecificTimes)
        /// chưa qua, sẽ lên lịch lại cho hôm nay; nếu đã qua, lên lịch cho ngày mai.
        /// </summary>
        public async Task ResumeScheduledCalls()
        {
            try
            {
                // Lấy tất cả các record lịch từ DB (collection SCHEDUL)
                var records = await _scheduledCallCollection.Find(Builders<ScheduleApiCallRequest>.Filter.Empty).ToListAsync();

                if (records == null || !records.Any())
                {
                    // Không có record nào, bỏ qua
                    return;
                }

                foreach (var record in records)
                {
                    if (record.SpecificTimes != null)
                    {
                        foreach (var timeStr in record.SpecificTimes)
                        {
                            if (TimeSpan.TryParse(timeStr, out TimeSpan ts))
                            {
                                // Tính thời gian dự kiến cho ngày hôm nay dựa trên giá trị ts
                                DateTime scheduledDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                    ts.Hours, ts.Minutes, 0);
                                // Nếu thời gian này đã qua, lên lịch cho ngày mai
                                if (scheduledDateTime < DateTime.Now)
                                    scheduledDateTime = scheduledDateTime.AddDays(1);
                                // Re-schedule job với Cron trigger hàng ngày, sử dụng .TimeOfDay
                                await ScheduleJobAt(record, scheduledDateTime.TimeOfDay);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is MongoException || ex is TimeoutException)
            {
                // Xử lý khi có lỗi kết nối MongoDB hoặc timeout
                // Có thể log lỗi ở đây nếu cần
                Console.WriteLine($"Không thể kết nối đến MongoDB: {ex.Message}");
                return; // Bỏ qua chức năng này và tiếp tục chương trình
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác nếu cần
                Console.WriteLine($"Lỗi khi resume scheduled calls: {ex.Message}");
                return; // Bỏ qua chức năng này và tiếp tục chương trình
            }
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

                // Kiểm tra xem lịch call cho đầu API này (theo URL) có còn tồn tại trong DB hay không
                bool isScheduled = await MonitorApiService.Instance.IsUrlScheduled(monitorApiModel.Url);
                if (!isScheduled)
                {
                    Console.WriteLine($"Không tìm thấy lịch call cho URL: {monitorApiModel.Url}. Bỏ qua việc call API.");
                    return;
                }

                // Nếu có lịch, tiến hành gọi API và lưu log (không ném exception nếu thất bại)
                await MonitorApiService.Instance.CallAndLog(monitorApiModel, throwOnFailure: false);
            }
        }
        #endregion
        #region GetPaging (phân trang)
        public async Task<dynamic> GetPaging(PagingParam pagingParam)
        {
            // Khởi tạo đối tượng kết quả phân trang.
            PagingModel<dynamic> result = new PagingModel<dynamic>();

            // Tạo bộ lọc (filter) cho MongoDB: bắt đầu với filter rỗng.
            var builder = Builders<MonitorApiModel>.Filter;
            var filter = builder.Empty;

            // Chỉ lấy những record mà IsDeleted = false.
            filter = builder.And(filter, builder.Eq("IsDeleted", false));

            // Nếu có lọc theo mã trạng thái (TrangThaiCode) được truyền vào,
            // thêm điều kiện lọc vào filter.
            if (!string.IsNullOrEmpty(pagingParam.TrangThaiCode))
            {
                filter = builder.And(filter, builder.Eq("TrangThai.Code", pagingParam.TrangThaiCode));
            }

            // Đếm tổng số bản ghi phù hợp với filter, lưu vào TotalRows.
            result.TotalRows = await _context.APIDB.CountDocumentsAsync(filter);

            // Xác định thứ tự sắp xếp dựa trên thuộc tính SortBy và SortDesc.
            // Nếu SortBy được set thành "Time", thì sắp xếp theo trường Time.
            // Nếu SortDesc là true, sắp xếp theo giảm dần (mới nhất trước); ngược lại, sắp xếp theo tăng dần (cũ nhất trước).
            IFindFluent<MonitorApiModel, MonitorApiModel> query = _context.APIDB.Find(filter);
            if (!string.IsNullOrEmpty(pagingParam.SortBy) && pagingParam.SortBy.Equals("Time", StringComparison.OrdinalIgnoreCase))
            {
                if (pagingParam.SortDesc)
                {
                    query = query.SortByDescending(x => x.Time);
                }
                else
                {
                    query = query.SortBy(x => x.Time);
                }
            }
            else
            {
                // Nếu SortBy không được set hoặc không phải "Time", sử dụng "CreatedAt" mặc định.
                // Giả sử mặc định sắp xếp theo CreatedAt giảm dần (mới nhất trước).
                query = query.SortByDescending(x => x.CreatedAt)
                             .ThenByDescending(x => x.CreatedAt);
            }

            // Áp dụng phân trang: bỏ qua số record tương ứng với Skip và giới hạn số record bằng Limit.
            result.Data = await query.Skip(pagingParam.Skip)
                                      .Limit(pagingParam.Limit)
                                      .ToListAsync();

            return result;
        }
        #endregion

        #region Get All Call History

        public async Task<List<MonitorApiModel>> GetAllCallHistory()
        {
            try
            {

                var filter = Builders<MonitorApiModel>.Filter.Empty;
                var builder = Builders<MonitorApiModel>.Filter;
                filter = builder.And(filter, builder.Eq("IsDeleted", false));
                var allRecords = await _context.APIDB.Find(filter).ToListAsync();


                // Sắp xếp API thất bại lên đầu, sau đó là theo thời gian giảm dần
                var sortedRecords = allRecords
                    .OrderBy(api => api.Code == "200") // Code khác 200 sẽ lên đầu (giả định thành công là "200")
                    .ThenByDescending(api => api.Time) // Sau đó sắp xếp theo thời gian giảm dần
                    .ToList();

                return sortedRecords;
            }
            catch (Exception ex)
            {
                throw new ResponseMessageException()
                    .WithCode(DefaultCode.EXCEPTION)
                    .WithMessage("Lỗi khi lấy lịch sử call: " + ex.Message);
            }
        }


        #endregion
        #region Get All Schedule

        public async Task<List<ScheduleApiCallRequest>> GetAllSchedule()
        {
            try
            {
                var filter = Builders<ScheduleApiCallRequest>.Filter.Empty;
                var allRecords = await _context.SCHEDUL.Find(filter).ToListAsync();
                return allRecords;
            }
            catch (Exception ex)
            {
                throw new ResponseMessageException()
                    .WithCode(DefaultCode.EXCEPTION)
                    .WithMessage("Lỗi khi lấy lịch call: " + ex.Message);
            }
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

        #region Check Scheduled API (Kiểm tra lịch theo URL)

        public async Task<bool> IsUrlScheduled(string url)
        {
            var filter = Builders<ScheduleApiCallRequest>.Filter.Eq(s => s.MonitorApiModel.Url, url);
            long count = await _scheduledCallCollection.CountDocumentsAsync(filter);
            return count > 0;
        }

        #endregion

        #region Implement IJobExecution (Interface Method)

        public async Task Execute(IJobExecutionContext context)
        {
            var job = new ApiCallJob();
            await job.Execute(context);
        }

        #endregion
        #region
        public async Task<byte[]> ExportCallHistoryToExcel()
        {
            try
            {
                var history = await GetAllCallHistory();

                // Kiểm tra dữ liệu đầu vào
                if (history == null || !history.Any())
                {
                    throw new ResponseMessageException()
                        .WithCode(DefaultCode.DATA_NOT_FOUND)
                        .WithMessage(DefaultMessage.DATA_NOT_FOUND);
                }

                string templatePath = Path.Combine(Directory.GetCurrentDirectory(),
                                                 "Properties",
                                                 "TemplateFiles",
                                                 "Lichsucall.xlsx");

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException("File template không tồn tại", templatePath);
                }

                using (var workbook = new XLWorkbook(templatePath))
                {
                    var worksheet = workbook.Worksheet("Sheet1");

                    // Xác định dòng bắt đầu (sau tiêu đề)
                    int startRow = 4;
                    int lastTemplateRow = worksheet.LastRowUsed()?.RowNumber() ?? startRow;

                    // Xóa dữ liệu cũ từ dòng startRow
                    if (lastTemplateRow >= startRow)
                    {
                        // Xóa dữ liệu từ cột A (STT) đến F (Code)
                        worksheet.Range(startRow, 1, lastTemplateRow, 6).Clear();
                    }

                    // Điền dữ liệu
                    int currentRow = startRow;
                    foreach (var record in history)
                    {
                        // Nếu vượt quá số dòng template, chèn thêm dòng mới
                        if (currentRow > lastTemplateRow)
                        {
                            // Chèn dòng mới và copy định dạng từ dòng trước đó
                            var rowToCopy = currentRow > startRow ? currentRow - 1 : startRow;
                            worksheet.Row(rowToCopy).CopyTo(worksheet.Row(currentRow));
                        }

                        // Điền số thứ tự (STT)
                        worksheet.Cell(currentRow, 1).Value = currentRow - startRow + 1;

                        // Điền dữ liệu vào các cột (phù hợp với template)
                        worksheet.Cell(currentRow, 2).Value = record.Name ?? "N/A";          // Cột B: Tên api
                        worksheet.Cell(currentRow, 3).Value = record.Url ?? "N/A";           // Cột C: Url
                        worksheet.Cell(currentRow, 4).Value = record.PhuongThuc?.Name ?? "N/A"; // Cột D: Phương thức
                        worksheet.Cell(currentRow, 5).Value = record.TrangThai?.Name ?? "N/A";  // Cột E: Trạng thái
                        worksheet.Cell(currentRow, 6).Value = record.Code ?? "N/A";           // Cột F: Code

                        // Áp dụng wrap text cho các ô mới
                        for (int col = 2; col <= 6; col++)
                        {
                            worksheet.Cell(currentRow, col).Style.Alignment.WrapText = true;
                        }

                        currentRow++;
                    }

                    // Xóa các dòng thừa nếu dữ liệu ít hơn template
                    if (currentRow < lastTemplateRow)
                    {
                        for (int row = lastTemplateRow; row >= currentRow; row--)
                        {
                            worksheet.Row(row).Delete();
                        }
                    }

                    // Tự động điều chỉnh độ rộng cột
                    worksheet.Columns().AdjustToContents();

                    // Định dạng bảng
                    var lastRow = Math.Max(currentRow - 1, startRow);
                    var range = worksheet.Range(startRow, 1, lastRow, 6);
                    range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
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
                if (ex.Message.Contains("Object reference not set to an instance of an object."))
                {
                    throw new ResponseMessageException()
                        .WithCode(DefaultCode.ERROR_STRUCTURE)
                        .WithMessage("Cấu trúc gói tin không đúng quy định!");
                }

                throw new ResponseMessageException()
                    .WithCode(DefaultCode.EXCEPTION)
                    .WithMessage(ex.Message);
            }
        }
        #endregion
    }
}