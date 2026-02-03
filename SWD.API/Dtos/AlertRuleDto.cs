namespace SWD.API.Dtos
{
    public class AlertRuleDto
    {
        public int RuleId { get; set; }
        public int SensorId { get; set; }
        public string? SensorName { get; set; }
        public int? SiteId { get; set; }
        public string? SiteName { get; set; }
        public int? HubId { get; set; }
        public string? HubName { get; set; }
        public string? Name { get; set; }
        public string ConditionType { get; set; } = null!;
        public double? MinVal { get; set; }
        public double? MaxVal { get; set; }
        public string? NotificationMethod { get; set; }
        public string? Priority { get; set; }
        public bool? IsActive { get; set; }
    }
}
