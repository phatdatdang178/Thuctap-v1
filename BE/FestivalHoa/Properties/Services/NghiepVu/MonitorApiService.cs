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
using System.Text;
using Newtonsoft.Json;



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
        public async Task<dynamic> Create(MonitorApiModel model)
        {
            try
            {
                // Kiểm tra đầu vào
                if (model == null || string.IsNullOrEmpty(model.Url))
                    throw new ResponseMessageException().WithException(DefaultCode.ERROR_STRUCTURE);

                // Xác định phương thức call API: nếu model.PhuongThuc có giá trị và Name là "POST" (không phân biệt chữ hoa/chữ thường)
                // thì sẽ gọi POST, ngược lại gọi GET.
                string methodName = "GET";
                if (model.PhuongThuc != null && !string.IsNullOrEmpty(model.PhuongThuc.Name))
                {
                    methodName = model.PhuongThuc.Name.ToUpper();
                }

                // Dùng trực tiếp cấu hình phương thức có trong model (không tạo mới vì đã có trong CSDL)
                // Nếu cần bổ sung kiểm tra từ CSDL thì có thể gọi _commonService.GetByNameAsync(...) ở đây.

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response;
                    if (methodName == "POST")
                    {
                        // Nếu BodyParams không phải chuỗi thì chuyển sang JSON
                        string jsonBody = "";
                        if (model.BodyParams != null)
                        {
                            if (model.BodyParams is string)
                                jsonBody = model.BodyParams;
                            else
                                jsonBody = JsonConvert.SerializeObject(model.BodyParams);
                        }
                        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        response = await client.PostAsync(model.Url, content);
                    }
                    else
                    {
                        response = await client.GetAsync(model.Url);
                    }

                    // Nếu API trả về lỗi (không thành công)
                    if (!response.IsSuccessStatusCode)
                    {
                        // Lấy trạng thái thất bại (TB) từ CSDL
                        var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                        {
                            Code = "TB", // Code cho trạng thái thất bại
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
                            // Lưu mã HTTPS trả về trong ghi chú
                            GhiChu = $"Call API thất bại với mã: {response.StatusCode}"
                        };

                        await BaseMongoDb.CreateAsync(logModel);
                        throw new ResponseMessageException()
                                .WithException(DefaultCode.DATA_EXISTED)
                                .WithMessage($"Call API thất bại với mã: {response.StatusCode}");
                    }
                    else
                    {
                        // Lấy trạng thái thành công (TC) từ CSDL
                        var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                        {
                            Code = "TC", // Code mặc định thành công
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
                            GhiChu = $"Call API thành công với mã: {response.StatusCode}"
                        };

                        ResultBaseMongo<MonitorApiModel> result = await BaseMongoDb.CreateAsync(logModel);
                        if (result.Entity.Id == default || !result.Success)
                            throw new ResponseMessageException().WithException(DefaultCode.CREATE_FAILURE);

                        return logModel;
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
                if (ex.Message.Contains("is not a valid 24 digit hex string."))
                {
                    throw new ResponseMessageException().WithException(DefaultCode.ID_NOT_CORRECT_FORMAT);
                }
                throw new ResponseMessageException().WithCode(DefaultCode.EXCEPTION).WithMessage(ex.Message);
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