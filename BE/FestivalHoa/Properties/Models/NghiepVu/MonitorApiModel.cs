using FestivalHoa.Properties.Models.Core;

namespace FestivalHoa.Properties.Models.NghiepVu
{
    public class MonitorApiModel : Audit, TEntity<string>
    {
      
            public string Url { get; set; }// api
            public int Start { get; set; } = 0;
            public int Limit { get; set; } = 0;
            public string ServiceId { get; set; }
            public CommonModelShort TrangThai { get; set; }
            public CommonModelShort PhuongThuc { get; set; }
            public string GhiChu { get; set; }
            public DateTime? Time { get; set; }
            public List<string> CallTimes { get; set; } = new(); // Danh sách thời gian (HH:mm)
            public string StartTime { get; set; }    // Ví dụ: "08:00"
            public string EndTime { get; set; }      // Ví dụ: "10:00"
            public int? CallFrequency { get; set; }  // Số lần gọi trong khoảng, ví dụ: 2
            public bool IsActive { get; set; } = true; // Cho phép bật/tắt lịch

    }
}
