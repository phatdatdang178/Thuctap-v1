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

namespace FestivalHoa.Properties.Services.NghiepVu
{
    public class MonitorApiService : BaseService, IMonitorApiService
    {
        private DataContext _context;
        private BaseMongoDb<MonitorApiModel, string> BaseMongoDb;
        private readonly IFileMinioService _fileMinioService;
        private readonly ICommonService _commonService;
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
        // debug chỗ này xem nó chạy ntn 
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

                        
                        var trangThaiEntity = await _commonService.GetByCodeAsync(new IdFromBodyCommonModel
                        {
                            Code = "TB", // Code cho trạng thái thất bại
                            CollectionName = "DM_TRANGTHAI"
                        });

                       
                        var test = new MonitorApiModel()
                        {
                            Id = BsonObjectId.GenerateNewId().ToString(),
                            Url = model.Url,
                            TrangThai =  new CommonModelShort
                            {
                                Id = trangThaiEntity.Id,
                                Code = trangThaiEntity.Code,
                                Name = trangThaiEntity.Name,
                            },
                            Time = model.Time,
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
                                 Id= trangThaiEntity.Id,
                                 Name = trangThaiEntity.Name,
                                 Code = trangThaiEntity.Code,
                                 
                             } , 
                            Time = model.Time,
                            PhuongThuc = model.PhuongThuc,
                            Start = model.Start,
                            Limit = model.Limit,
                            GhiChu = model.GhiChu,
                        };

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
            // Kiểm tra đầu vào: bắt buộc phải có URL API
            if (model == null || string.IsNullOrEmpty(model.Url))
                throw new ResponseMessageException()
                    .WithException(DefaultCode.ERROR_STRUCTURE)
                    .WithMessage("Cần nhập URL API cụ thể để thiết lập giờ call tự động.");

            // Lấy giờ hiện tại theo định dạng "HH:mm"
            string currentTime = DateTime.Now.ToString("HH:mm");

            // Kiểm tra nếu giờ hiện tại nằm trong danh sách giờ call đã cấu hình
            if (model.CallTimes != null && model.CallTimes.Any(t => t.Equals(currentTime, StringComparison.OrdinalIgnoreCase)))
            {
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
                            Time = DateTime.Now,
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
                            Time = DateTime.Now,
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
                // Nếu không đến giờ gọi, trả về thông báo phù hợp
                return new ResultMessageResponse()
                    .WithMessage("Chưa đến giờ call tự động hoặc không có cấu hình giờ call phù hợp.")
                    .WithCode(DefaultCode.SUCCESS);
            }
        }



        public async Task<dynamic> GetPaging(PagingParam pagingParam)
        {
            PagingModel<dynamic> result = new PagingModel<dynamic>();
            var builder = Builders<HoaModel>.Filter;
            var filter = builder.Empty;
            filter = builder.And(filter, builder.Eq("IsDeleted", false));
            result.TotalRows = await _context.HOA.CountDocumentsAsync(filter);

            string sortBy = pagingParam.SortBy != null ? FormatterString.HandlerSortBy(pagingParam.SortBy) : "CreatedAt";
            result.Data = await _context.HOA.Find(filter)
                .SortByDescending(x=>x.CreatedAt)
                .ThenByDescending(e => e.CreatedAt)
                .Skip(pagingParam.Skip)
                .Limit(pagingParam.Limit)
                .ToListAsync();
            
                
            return result;
        }

    }
}