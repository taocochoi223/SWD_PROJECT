namespace SWD.API.Dtos
{
    public class UpdateAlertRuleDto
    {
        public string? Name { get; set; }
        public string? ConditionType { get; set; }
        public double? MinVal { get; set; }
        public double? MaxVal { get; set; }
        public string? NotificationMethod { get; set; }
        public string? Priority { get; set; }
        public bool? IsActive { get; set; }
    }
}