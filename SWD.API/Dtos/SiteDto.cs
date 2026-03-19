namespace SWD.API.Dtos
{
    public class SiteDto
    {
        public int SiteId { get; set; }
        public int OrgId { get; set; }
        public string? OrgName { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? GeoLocation { get; set; }
        public int HubCount { get; set; }
        public List<HubSummaryDto> Hubs { get; set; } = new List<HubSummaryDto>();
    }

    public class HubSummaryDto
    {
        public int HubId { get; set; }
        public string? Name { get; set; }
        public bool? IsOnline { get; set; }
    }
}
