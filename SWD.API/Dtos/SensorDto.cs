namespace SWD.API.Dtos
{
    public class SensorDto
    {
        public int SensorId { get; set; }
        public int HubId { get; set; }
        public string? HubName { get; set; }
        public int TypeId { get; set; }
        public string? TypeName { get; set; }
        public string? SensorName { get; set; }
        public double? CurrentValue { get; set; }
        public string? Status { get; set; }
    }
}
