namespace SWD.API.Dtos
{
    public class NotificationDto
    {
        public long NotiId { get; set; }
        public int RuleId { get; set; }
        public int UserId { get; set; }
        public string? Message { get; set; }
        public DateTime? SentAt { get; set; }
        public bool? IsRead { get; set; }
        
        // Alert info
        public int? SensorId { get; set; }
        public string? SensorName { get; set; }
        public string? Severity { get; set; }
        public DateTime? TriggeredAt { get; set; }
    }
}
