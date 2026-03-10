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

    public virtual Hub Hub { get; set; } = null!;

    public virtual SensorType Type { get; set; } = null!;
}
