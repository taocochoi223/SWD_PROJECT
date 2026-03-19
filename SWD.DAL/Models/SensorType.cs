using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class SensorType
{
    public int TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    public virtual ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
}
