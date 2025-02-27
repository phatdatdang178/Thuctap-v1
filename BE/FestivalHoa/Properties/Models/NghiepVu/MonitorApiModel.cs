using System;
using System.Collections.Generic;
using FestivalHoa.Properties.Converters;
using FestivalHoa.Properties.Models.Core;
using Newtonsoft.Json;

namespace FestivalHoa.Properties.Models.NghiepVu
{
    public class MonitorApiModel : Audit, TEntity<string>
    {
        public string Url { get; set; } // API endpoint
        public string ServiceId { get; set; }
        public CommonModelShort TrangThai { get; set; }
        public CommonModelShort PhuongThuc { get; set; }
        // Áp dụng converter để chuyển đổi dữ liệu vào BodyParams
        [JsonConverter(typeof(ObjectToStringJsonConverter))]
        public string BodyParams { get; set; }
        public string GhiChu { get; set; }
        public string Code { get; set; }
        public DateTime? Time { get; set; }
        public List<string> CallTimes { get; set; } = new(); // Danh sách thời gian (HH:mm)
        public bool IsActive { get; set; } = true; // Cho phép bật/tắt lịch
    }
}
