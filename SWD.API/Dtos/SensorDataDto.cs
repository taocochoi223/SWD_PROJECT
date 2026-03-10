namespace SWD.API.Dtos
{
    public class SensorDataDto
    {
        public long DataId { get; set; }
        public int HubId { get; set; }
        public string JsonValue { get; set; } = null!;
        public DateTime? RecordedAt { get; set; }
    }
}
