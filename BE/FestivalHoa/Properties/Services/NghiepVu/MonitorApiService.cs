using FestivalHoa.Properties.Services.Core;
using FestivalHoa.Properties.Interfaces.NghiepVu;
using FestivalHoa.Properties.Extensions;
using FestivalHoa.Properties.Exceptions;
using FestivalHoa.Properties.Helpers;
using FestivalHoa.Properties.Installers;
using FestivalHoa.Properties.Models.CongDan;
using MongoDB.Driver;
using MongoDB.Bson;
using FestivalHoa.Properties.Interfaces.Core;
using ZXing;
using System.Drawing;
using FestivalHoa.Properties.Constants;
using FestivalHoa.Properties.Models.PagingParam;
using MongoDB.Bson.Serialization;
using FestivalHoa.Properties.Models.Core;
using FestivalHoa.Properties.Models.NghiepVu;
using FestivalHoa.Properties.FromBodyModels;
using FestivalHoa.Properties.Interfaces.Common;
using FestivalHoa.Properties.Services.Common;
using System.Globalization;



namespace FestivalHoa.Properties.Services.NghiepVu
{
    public class MonitorApiService : BaseService, IMonitorApiService
    {
        private DataContext _context;
        private BaseMongoDb<MonitorApiModel, string> BaseMongoDb;
        private readonly IFileMinioService _fileMinioService;
        private readonly ICommonService _commonService;
        private readonly IMongoCollection<MonitorApiModel> _callHistories;
        public MonitorApiService(
            DataContext context,
            IHttpContextAccessor contextAccessor,
            IFileMinioService fileMinioService,
             ICommonService commonService
            ) :
            base(context, contextAccessor)
        {
            _context = context;
            BaseMongoDb = new BaseMongoDb<MonitorApiModel, string>(_context.TEST);
            _fileMinioService = fileMinioService;
            _commonService = commonService;
        }
 //ngoai url con 1 cai
        // thêm cấu hình thời gian call
        public async Task<dynamic> Create(MonitorApiModel model)
        {
            try
            {
                if (model == default || string.IsNullOrEmpty(model.Url))
                    throw new ResponseMessageException().WithException(DefaultCode.ERROR_STRUCTURE);

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(model.Url);
                    if (!response.IsSuccessStatusCode)
                    {

                        //check phuong thuc post hay get
                        var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                        {
                            Code = "TB", // Code cho trạng thái thất bại
                            CollectionName = "DM_TRANGTHAI"
                        });


                        var test = new MonitorApiModel()
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
                            Start = model.Start,
                            Limit = model.Limit,
                            GhiChu = model.GhiChu,
                        };

                        await BaseMongoDb.CreateAsync(test);

                        throw new ResponseMessageException().WithException(DefaultCode.DATA_EXISTED)
                            .WithMessage($"Call API thất bại {response.StatusCode}");
                        // kiểm tra xem nếu thất bại thì lưu trang thái là thất bại tương tự như thành công
                    }
                    else
                    {
                        // trường hợp call api trả về thành công đã set rồi
                        var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                        {
                            Code = "TC", // Code mặc định
                            CollectionName = "DM_TRANGTHAI" // Đổi tên collection cho phù hợp 
                        });
                        var test = new MonitorApiModel()
                        {
                            Id = BsonObjectId.GenerateNewId().ToString(),
                            Url = model.Url,
                            TrangThai = new CommonModelShort
                            {
                                Id = trangThaiEntity.Id,
                                Name = trangThaiEntity.Name,
                                Code = trangThaiEntity.Code,

                            },
                            Time = DateTime.UtcNow.AddHours(7),
                            CallTimes = model.CallTimes,
                            Name = model.Name,
                            PhuongThuc = model.PhuongThuc,
                            Start = model.Start,
                            Limit = model.Limit,
                            GhiChu = model.GhiChu,
                        };
                        //luu code trang thai 
                        //
                        ResultBaseMongo<MonitorApiModel> result = await BaseMongoDb.CreateAsync(test);
                        if (result.Entity.Id == default || !result.Success)
                            throw new ResponseMessageException().WithException(DefaultCode.CREATE_FAILURE);

                        return test;
                    }
                }


            }
            catch (ResponseMessageException e)
            {
                throw new ResponseMessageException().WithCode(DefaultCode.EXCEPTION).WithMessage(e.ResultString).WithDetail(e.Error);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("is not a valid 24 digit hex string."))
                {
                    throw new ResponseMessageException().WithException(DefaultCode.ID_NOT_CORRECT_FORMAT);
                }

                throw new ResponseMessageException().WithCode(DefaultCode.EXCEPTION).WithMessage(ex.Message);
            }
        }


        public async Task<dynamic> AutoCall(MonitorApiModel model)
        {
            // Kiểm tra đầu vào: phải có URL API
            if (model == null || string.IsNullOrEmpty(model.Url))
                throw new ResponseMessageException()
                    .WithException(DefaultCode.ERROR_STRUCTURE)
                    .WithMessage("Cần nhập URL API!");

            // Lấy giờ hiện tại theo giờ Việt Nam (GMT+7)
            DateTime now = DateTime.UtcNow.AddHours(7);

            // Nếu cấu hình khoảng thời gian (StartTime, EndTime) và số lần gọi (CallFrequency) có giá trị thì ưu tiên sử dụng cấu hình này
            if (!string.IsNullOrEmpty(model.StartTime) && !string.IsNullOrEmpty(model.EndTime)
                && model.CallFrequency.HasValue && model.CallFrequency.Value > 0)
            {
                // Parse StartTime, EndTime theo định dạng "HH:mm"
                DateTime startTime = DateTime.ParseExact(model.StartTime, "HH:mm", CultureInfo.InvariantCulture);
                DateTime endTime = DateTime.ParseExact(model.EndTime, "HH:mm", CultureInfo.InvariantCulture);

                // Gán cho ngày hôm nay (hoặc chuyển sang ngày mai nếu khoảng thời gian đã qua)
                DateTime scheduledStart = new DateTime(now.Year, now.Month, now.Day, startTime.Hour, startTime.Minute, 0);
                DateTime scheduledEnd = new DateTime(now.Year, now.Month, now.Day, endTime.Hour, endTime.Minute, 0);
                if (scheduledEnd < now)
                {
                    scheduledStart = scheduledStart.AddDays(1);
                    scheduledEnd = scheduledEnd.AddDays(1);
                }

                int frequency = model.CallFrequency.Value;
                // Nếu chỉ gọi 1 lần thì gọi vào thời điểm bắt đầu;
                // nếu gọi nhiều lần, chia đều khoảng thời gian (bao gồm điểm bắt đầu và kết thúc)
                TimeSpan interval = frequency > 1 ? TimeSpan.FromTicks((scheduledEnd - scheduledStart).Ticks / (frequency - 1)) : TimeSpan.Zero;

                // Lặp qua từng lần gọi
                for (int i = 0; i < frequency; i++)
                {
                    DateTime scheduledCallTime = scheduledStart.AddTicks(interval.Ticks * i);
                    if (scheduledCallTime < now)
                        continue; // Bỏ qua nếu thời điểm đã qua

                    // Tạo đối tượng lịch gọi và lưu vào DB (sử dụng collection SCHEDUL của DataContext)
                    var scheduledCall = new ScheduledCallModel()
                    {
                        Id = BsonObjectId.GenerateNewId().ToString(),
                        Url = model.Url,
                        ScheduledTime = scheduledCallTime,
                        Status = "Scheduled"
                    };
                    await _context.SCHEDUL.InsertOneAsync(scheduledCall);

                    // Đặt tác vụ nền để thực hiện gọi API vào thời điểm đã tính
                    _ = Task.Run(async () =>
                    {
                        // Tính khoảng delay lại theo giờ Việt Nam
                        TimeSpan delay = scheduledCallTime - DateTime.UtcNow.AddHours(7);
                        if (delay.TotalMilliseconds > 0)
                            await Task.Delay(delay);

                        using (HttpClient client = new HttpClient())
                        {
                            HttpResponseMessage response = await client.GetAsync(model.Url);
                            if (response.IsSuccessStatusCode)
                            {
                                // Lấy trạng thái thành công (TC)
                                var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                                {
                                    Code = "TC",
                                    CollectionName = "DM_TRANGTHAI"
                                });
                                // Lưu kết quả gọi API vào MonitorApiModel
                                var callResult = new MonitorApiModel()
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
                                    Start = model.Start,
                                    Limit = model.Limit,
                                    GhiChu = model.GhiChu,
                                };
                                await BaseMongoDb.CreateAsync(callResult);

                                // Cập nhật lịch gọi thành công
                                scheduledCall.Status = "Executed";
                                scheduledCall.ExecutionTime = DateTime.UtcNow.AddHours(7);
                                scheduledCall.Result = "Call API thành công";
                            }
                            else
                            {
                                // Lấy trạng thái thất bại (TB)
                                var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                                {
                                    Code = "TB",
                                    CollectionName = "DM_TRANGTHAI"
                                });
                                var callResult = new MonitorApiModel()
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
                                    Start = model.Start,
                                    Limit = model.Limit,
                                    GhiChu = model.GhiChu,
                                };
                                await BaseMongoDb.CreateAsync(callResult);

                                // Cập nhật lịch gọi thất bại
                                scheduledCall.Status = "Failed";
                                scheduledCall.ExecutionTime = DateTime.UtcNow.AddHours(7);
                                scheduledCall.Result = $"Call API thất bại {response.StatusCode}";
                            }
                            // Cập nhật trạng thái lịch gọi trong DB
                            var filter = Builders<ScheduledCallModel>.Filter.Eq(x => x.Id, scheduledCall.Id);
                            await _context.SCHEDUL.ReplaceOneAsync(filter, scheduledCall);
                        }
                    });
                }

                return new ResultMessageResponse()
                    .WithMessage($"Đã đặt lịch gọi API từ {model.StartTime} đến {model.EndTime} với tần suất {frequency} lần.")
                    .WithCode(DefaultCode.SUCCESS);
            }
            else
            {
                // Nếu không có cấu hình StartTime-EndTime-CallFrequency, sử dụng danh sách CallTimes như cũ
                string currentTime = now.ToString("HH:mm");
                if (model.CallTimes != null && model.CallTimes.Any(t => t.Equals(currentTime, StringComparison.OrdinalIgnoreCase)))
                {
                    using (HttpClient client = new HttpClient())
                    {
                        HttpResponseMessage response = await client.GetAsync(model.Url);
                        if (response.IsSuccessStatusCode)
                        {
                            var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                            {
                                Code = "TC",
                                CollectionName = "DM_TRANGTHAI"
                            });
                            var callResult = new MonitorApiModel()
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
                                Start = model.Start,
                                Limit = model.Limit,
                                GhiChu = model.GhiChu,
                            };

                            ResultBaseMongo<MonitorApiModel> result = await BaseMongoDb.CreateAsync(callResult);
                            if (result.Entity.Id == default || !result.Success)
                                throw new ResponseMessageException().WithException(DefaultCode.CREATE_FAILURE);

                            return callResult;
                        }
                        else
                        {
                            var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                            {
                                Code = "TB",
                                CollectionName = "DM_TRANGTHAI"
                            });
                            var callResult = new MonitorApiModel()
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
                                Start = model.Start,
                                Limit = model.Limit,
                                GhiChu = model.GhiChu,
                            };

                            await BaseMongoDb.CreateAsync(callResult);
                            throw new ResponseMessageException()
                                .WithException(DefaultCode.DATA_EXISTED)
                                .WithMessage($"Call API thất bại {response.StatusCode}");
                        }
                    }
                }
                else
                {
                    // Tính toán thời gian gọi API tiếp theo dựa trên danh sách CallTimes
                    DateTime? nextScheduledTime = null;
                    foreach (var timeStr in model.CallTimes)
                    {
                        if (DateTime.TryParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime scheduledTime))
                        {
                            DateTime scheduledToday = new DateTime(now.Year, now.Month, now.Day, scheduledTime.Hour, scheduledTime.Minute, 0);
                            if (scheduledToday < now)
                                scheduledToday = scheduledToday.AddDays(1);
                            if (nextScheduledTime == null || scheduledToday < nextScheduledTime.Value)
                                nextScheduledTime = scheduledToday;
                        }
                    }

                    if (nextScheduledTime.HasValue)
                    {
                        TimeSpan delay = nextScheduledTime.Value - now;
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(delay);
                            using (HttpClient client = new HttpClient())
                            {
                                HttpResponseMessage response = await client.GetAsync(model.Url);
                                if (response.IsSuccessStatusCode)
                                {
                                    var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                                    {
                                        Code = "TC",
                                        CollectionName = "DM_TRANGTHAI"
                                    });
                                    var callResult = new MonitorApiModel()
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
                                        Start = model.Start,
                                        Limit = model.Limit,
                                        GhiChu = model.GhiChu,
                                    };

                                    await BaseMongoDb.CreateAsync(callResult);
                                }
                                else
                                {
                                    var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                                    {
                                        Code = "TB",
                                        CollectionName = "DM_TRANGTHAI"
                                    });
                                    var callResult = new MonitorApiModel()
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
                                        Start = model.Start,
                                        Limit = model.Limit,
                                        GhiChu = model.GhiChu,
                                    };

                                    await BaseMongoDb.CreateAsync(callResult);
                                }
                            }
                        });

                        return new ResultMessageResponse()
                            .WithMessage($"Đã đặt lịch gọi API vào lúc {nextScheduledTime.Value.ToString("HH:mm")}.")
                            .WithCode(DefaultCode.SUCCESS);
                    }
                    else
                    {
                        return new ResultMessageResponse()
                            .WithMessage("Không có lịch gọi API hợp lệ.")
                            .WithCode(DefaultCode.SUCCESS);
                    }
                }
            }
        }

        public async Task<dynamic> GetPaging(PagingParam pagingParam)
        {
            PagingModel<dynamic> result = new PagingModel<dynamic>();
            var builder = Builders<MonitorApiModel>.Filter;
            var filter = builder.Empty;
            filter = builder.And(filter, builder.Eq("IsDeleted", false));
            if (pagingParam.TrangThaiCode != null && !pagingParam.TrangThaiCode.Equals(""))
            {
                filter = builder.And(filter,
                    builder.Eq("TrangThai.Code", pagingParam.TrangThaiCode)
                );
            }
          
            result.TotalRows = await _context.TEST.CountDocumentsAsync(filter);


            string sortBy = pagingParam.SortBy != null ? FormatterString.HandlerSortBy(pagingParam.SortBy) : "CreatedAt";
            result.Data = await _context.TEST.Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .ThenByDescending(e => e.CreatedAt)
                .Skip(pagingParam.Skip)
                .Limit(pagingParam.Limit)
                .ToListAsync();


            return result;
        }

    }
}