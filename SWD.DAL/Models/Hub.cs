using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class Hub
{
    public int HubId { get; set; }

    public int SiteId { get; set; }

    public string? Name { get; set; }

    public string MacAddress { get; set; } = null!;

    public bool? IsOnline { get; set; }

    public DateTime? LastHandshake { get; set; }

    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();

    public virtual ICollection<SensorData> SensorDatas { get; set; } = new List<SensorData>();

    public virtual Site Site { get; set; } = null!;
}
