using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD.BLL.Interfaces;
using SWD.API.Dtos;
using System.Linq;

namespace SWD.API.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ISiteService _siteService;
        private readonly IHubService _hubService;
        private readonly ISensorService _sensorService;
        private readonly IAlertService _alertService;

        public DashboardController(
            ISiteService siteService,
            IHubService hubService,
            ISensorService sensorService,
            IAlertService alertService)
        {
            _siteService = siteService;
            _hubService = hubService;
            _sensorService = sensorService;
            _alertService = alertService;
        }

        /// <summary>
        /// Get Dashboard Statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStatsAsync()
        {
            try
            {
                var sites = await _siteService.GetAllSitesAsync();
                var hubs = await _hubService.GetAllHubsAsync();
                var sensors = await _sensorService.GetAllSensorsAsync();
                var alerts = new List<object>(); // Placeholder as AlertHistory is removed
                // var alerts = await _alertService.GetAlertsWithFiltersAsync("Active", null);

                var stats = new
                {
                    message = "Lấy thống kê dashboard thành công",
                    total_sites = sites.Count,
                    total_hubs = hubs.Count,
                    active_sensors = sensors.Count(s => s.Status == "Active"),
                    pending_alerts = 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thống kê: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Hierarchy (Site -> Hub -> Sensor)
        /// </summary>
        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetHierarchyAsync()
        {
            try
            {
                var sites = await _siteService.GetSiteHierarchyAsync();
                
                var siteDtos = sites.Select(s => new SiteDashboardDto
                {
                    SiteId = s.SiteId,
                    Name = s.Name,
                    Address = s.Address,
                    Hubs = s.Hubs?.Select(h => new HubDashboardDto
                    {
                        HubId = h.HubId,
                        Name = h.Name,
                        MacAddress = h.MacAddress,
                        IsOnline = h.IsOnline,
                        LastHandshake = h.LastHandshake,
                        Sensors = h.Sensors?.Select(se => new SensorDashboardDto
                        {
                            SensorId = se.SensorId,
                            Name = se.Name,
                            TypeName = se.Type?.TypeName ?? "Unknown",
                            Unit = se.Type?.Unit ?? "",
                            CurrentValue = (float?)(se.SensorDatas?.OrderByDescending(d => d.RecordedAt).FirstOrDefault()?.Value ?? 0),
                            LastUpdate = se.SensorDatas?.OrderByDescending(d => d.RecordedAt).FirstOrDefault()?.RecordedAt,
                            TotalReadings = se.SensorDatas?.Count ?? 0
                        }).ToList() ?? new List<SensorDashboardDto>()
                    }).ToList() ?? new List<HubDashboardDto>()
                }).ToList();

                return Ok(new { message = "Lấy cấu trúc phân cấp thành công", data = siteDtos });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy cấu trúc phân cấp: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Hierarchy for specific Site (Site -> Hub -> Sensor)
        /// </summary>
        [HttpGet("site/{siteId}")]
        public async Task<IActionResult> GetHierarchyBySiteIdAsync(int siteId)
        {
            try
            {
                // Validate siteId
                if (siteId <= 0)
                    return BadRequest(new { message = "SiteId không hợp lệ" });

                var s = await _siteService.GetSiteHierarchyByIdAsync(siteId);
                if (s == null)
                {
                    return NotFound(new { message = "Không tìm thấy địa điểm với ID: " + siteId });
                }

                var siteDto = new SiteDashboardDto
                {
                    SiteId = s.SiteId,
                    Name = s.Name,
                    Address = s.Address,
                    Hubs = s.Hubs?.Select(h => new HubDashboardDto
                    {
                        HubId = h.HubId,
                        Name = h.Name,
                        MacAddress = h.MacAddress,
                        IsOnline = h.IsOnline,
                        LastHandshake = h.LastHandshake,
                        Sensors = h.Sensors?.Select(se => new SensorDashboardDto
                        {
                            SensorId = se.SensorId,
                            Name = se.Name,
                            TypeName = se.Type?.TypeName ?? "Unknown",
                            Unit = se.Type?.Unit ?? "",
                            CurrentValue = (float?)(se.SensorDatas?.OrderByDescending(d => d.RecordedAt).FirstOrDefault()?.Value ?? 0),
                            LastUpdate = se.SensorDatas?.OrderByDescending(d => d.RecordedAt).FirstOrDefault()?.RecordedAt,
                            TotalReadings = se.SensorDatas?.Count ?? 0
                        }).ToList() ?? new List<SensorDashboardDto>()
                    }).ToList() ?? new List<HubDashboardDto>()
                };

                var hubCount = siteDto.Hubs.Count;
                var message = hubCount > 0 
                    ? "Lấy thông tin dashboard theo địa điểm thành công" 
                    : "Lấy thông tin thành công nhưng địa điểm này chưa có Hub nào";

                return Ok(new { 
                    message = message, 
                    data = siteDto,
                    hubCount = hubCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thông tin dashboard: " + ex.Message });
            }
        }
    }
}
