using System.Collections.Generic;
namespace FestivalHoa.Properties.Models.NghiepVu
{
    public class ScheduleApiCallRequest
    {
        // Cấu hình API call (đã bao gồm URL, phương thức, bodyParams,...)
        public MonitorApiModel MonitorApiModel { get; set; }
        // Cách 1: Các thời gian gọi cụ thể (định dạng "HH:mm")
        public List<string> SpecificTimes { get; set; }
        // Cách 2: Cấu hình theo khoảng thời gian
        public string StartTime { get; set; }    // Ví dụ: "08:00"
        public string EndTime { get; set; }      // Ví dụ: "10:00"
        public int? CallFrequency { get; set; }  // Số lần gọi trong khoảng, ví dụ: 2
    }
}
