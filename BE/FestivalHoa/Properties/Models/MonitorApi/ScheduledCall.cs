using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace FestivalHoa.Properties.Models.MonitorApi
{
    public class ScheduledCall
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public List<string> CallTimes { get; set; } = new(); // Danh sách thời gian (HH:mm)

        // Các trường mới cho cấu hình theo khoảng thời gian
        public string StartTime { get; set; }    // Ví dụ: "08:00"
        public string EndTime { get; set; }      // Ví dụ: "10:00"
        public int? CallFrequency { get; set; }  // Số lần gọi trong khoảng, ví dụ: 2
        public bool IsActive { get; set; } = true; // Cho phép bật/tắt lịch

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
