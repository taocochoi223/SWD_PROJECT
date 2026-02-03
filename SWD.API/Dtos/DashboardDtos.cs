
using System.Collections.Generic;

namespace SWD.API.Dtos
{
    public class SiteDashboardDto
    {
        public int SiteId { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public List<HubDashboardDto> Hubs { get; set; } = new();
    }

    public class HubDashboardDto
    {
        public int HubId { get; set; }
        public string Name { get; set; } = null!;
        public string MacAddress { get; set; } = null!;
        public bool? IsOnline { get; set; }
        public DateTime? LastHandshake { get; set; }
        public List<SensorDashboardDto> Sensors { get; set; } = new();
    }

    public class SensorDashboardDto
    {
        public int SensorId { get; set; }
        public string? Name { get; set; }
        public string TypeName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public float? CurrentValue { get; set; }
        public DateTime? LastUpdate { get; set; }
        public int TotalReadings { get; set; }
    }

    public class DashboardChartDto 
    {
        

    }
}
