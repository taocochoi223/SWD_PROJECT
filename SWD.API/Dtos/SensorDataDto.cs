namespace SWD.API.Dtos
{
    public class SensorDataDto
    {
        public long DataId { get; set; }
        public int SensorId { get; set; }
        public int HubId { get; set; }
        public string? SensorName { get; set; }
        public string? SensorTypeName { get; set; }
        public double Value { get; set; }
        public DateTime? RecordedAt { get; set; }
    }
}
