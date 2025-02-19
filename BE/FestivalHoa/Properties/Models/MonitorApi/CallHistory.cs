using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace FestivalHoa.Properties.Models.MonitorApi
{
    public class CallHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ApiUrl { get; set; }

        public string Method { get; set; }

        public string RequestPayload { get; set; }

        public DateTime CallTime { get; set; }

        public string Note { get; set; }

        public string Status { get; set; }

        public int StatusCode { get; set; }
    }
}
