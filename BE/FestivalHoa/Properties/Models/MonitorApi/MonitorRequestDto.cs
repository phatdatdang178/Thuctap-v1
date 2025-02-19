namespace FestivalHoa.Properties.Models.MonitorApi
{
    public class MonitorRequestDto
    {
        public string ApiUrl { get; set; }
        public string Method { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object>? Payload { get; set; }
        public string Note { get; set; }
    }
}
