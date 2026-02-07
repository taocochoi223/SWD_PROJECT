using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class AlertRule
{
    public int RuleId { get; set; }

    public int SensorId { get; set; }

    public string? Name { get; set; }

    public string ConditionType { get; set; } = null!;

    public double? MinVal { get; set; }

    public double? MaxVal { get; set; }

    public string? NotificationMethod { get; set; }

    public string? Priority { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Sensor Sensor { get; set; } = null!;
}
