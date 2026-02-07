using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class Sensor
{
    public int SensorId { get; set; }

    public int HubId { get; set; }

    public int TypeId { get; set; }

    public string? Name { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();

    public virtual Hub Hub { get; set; } = null!;

    public virtual ICollection<SensorData> SensorDatas { get; set; } = new List<SensorData>();

    public virtual SensorType Type { get; set; } = null!;
}
