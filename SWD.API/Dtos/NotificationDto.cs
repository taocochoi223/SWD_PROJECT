namespace SWD.API.Dtos
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public int? RuleId { get; set; }
        public int? UserId { get; set; }
        public string? Message { get; set; }
        public string? SensorName { get; set; }
        public string? Location { get; set; }
        public double? Value { get; set; }
        public string? MetricUnit { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; } = "Active";
        public DateTime? Time { get; set; }
        public bool? IsRead { get; set; }
        
        // Raw data for filters if needed
        public int? SensorId { get; set; }
    }
}
