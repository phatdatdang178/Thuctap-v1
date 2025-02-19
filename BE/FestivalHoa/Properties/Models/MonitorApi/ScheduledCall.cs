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

        public bool IsActive { get; set; } = true; // Cho phép bật/tắt lịch

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
