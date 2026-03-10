using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class SensorData
{
    public long DataId { get; set; }

    public int HubId { get; set; }

    public string JsonValue { get; set; } = null!;

    public DateTime? RecordedAt { get; set; }

    public virtual Hub Hub { get; set; } = null!;
}
