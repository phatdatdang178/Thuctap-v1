using FestivalHoa.Properties.Models.Core;

namespace FestivalHoa.Properties.Models.NghiepVu
{
    public class ScheduledCallModel : Audit, TEntity<string>
    {
        public string Url { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; } = "Scheduled"; // Các trạng thái: Scheduled, Executed, Failed
        public DateTime? ExecutionTime { get; set; }
        public string Result { get; set; }
    }
}
