using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class SensorData
{
    public long DataId { get; set; }

    public int SensorId { get; set; }

    public int HubId { get; set; }

    public double Value { get; set; }

    public DateTime? RecordedAt { get; set; }

    public virtual Sensor Sensor { get; set; } = null!;

    public virtual Hub Hub { get; set; } = null!;
}
