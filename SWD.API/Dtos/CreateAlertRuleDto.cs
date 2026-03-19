using System.ComponentModel.DataAnnotations;

namespace SWD.API.Dtos
{
    public class CreateAlertRuleDto
    {
        [Required]
        public int OrgId { get; set; }

        [Required]
        public int HubId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [RegularExpression("MinMax|Range|Greater|Less|Trend", ErrorMessage = "ConditionType must be 'MinMax', 'Range', 'Greater', 'Less' or 'Trend'")]
        public string ConditionType { get; set; } // 'MinMax', 'Range', 'Greater', 'Less', 'Trend'

        public double? MinVal { get; set; }

        public double? MaxVal { get; set; }

        public string NotificationMethod { get; set; } = "Email";

        public string Priority { get; set; } = "Warning";
    }
}