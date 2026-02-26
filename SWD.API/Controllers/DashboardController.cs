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
        private readonly INotificationService _notiService;

        public DashboardController(
            ISiteService siteService,
            IHubService hubService,
            ISensorService sensorService,
            IAlertService alertService,
            INotificationService notiService)
        {
            _siteService = siteService;
            _hubService = hubService;
            _sensorService = sensorService;
            _alertService = alertService;
            _notiService = notiService;
        }

        /// <summary>
        /// Get Dashboard Statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStatsAsync()
        {
            try
            {
                var (sites, _) = await _siteService.GetAllSitesAsync();
                var (hubs, _) = await _hubService.GetAllHubsAsync();
                var (sensors, _) = await _sensorService.GetAllSensorsAsync();
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

        /// <summary>
        /// Get Current Environment Data - Lấy dữ liệu môi trường hiện tại của Hub (Temperature, Humidity, Pressure)
        /// </summary>
        [HttpGet("hub/{id}/current-environment")]
        public async Task<IActionResult> GetCurrentEnvironmentDataAsync(int id)
        {
            try
            {
                var hub = await _hubService.GetHubByIdAsync(id);
                if (hub == null)
                    return NotFound(new { message = "Không tìm thấy Hub với ID: " + id });

                // KIỂM TRA PHÂN QUYỀN
                var siteIdClaim = User.FindFirst("SiteId")?.Value;
                int? userSiteId = !string.IsNullOrEmpty(siteIdClaim) ? int.Parse(siteIdClaim) : null;

                if (userSiteId.HasValue && hub.SiteId != userSiteId.Value)
                {
                    return StatusCode(403, new { message = "Bạn không có quyền truy cập Hub này" });
                }

                var envSensors = await _hubService.GetHubCurrentTemperatureAsync(id);

                if (!envSensors.Any())
                    return NotFound(new { message = "Hub này không có cảm biến môi trường (Temperature/Humidity/Pressure)" });

                var result = new HubReadingsDto
                {
                    HubId = hub.HubId,
                    Name = hub.Name,
                    MacAddress = hub.MacAddress,
                    Sensors = envSensors.Select(s => new SensorReadingDto
                    {
                        SensorId = s.SensorId,
                        Name = s.Name,
                        TypeName = s.Type?.TypeName ?? "Unknown",
                        Unit = s.Type?.Unit ?? "",
                        Readings = s.SensorDatas?.OrderByDescending(d => d.RecordedAt).Take(1).Select(r => new ReadingValueDto
                        {
                            RecordedAt = r.RecordedAt ?? DateTime.MinValue,
                            Value = (float)r.Value
                        }).ToList() ?? new List<ReadingValueDto>()
                    }).ToList()
                };

                return Ok(new
                {
                    message = "Lấy dữ liệu môi trường hiện tại của Hub thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu môi trường: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Latest Alerts for Dashboard
        /// </summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetLatestAlertsAsync([FromQuery] int limit = 5)
        {
            try
            {
                // Lấy UserId từ Token
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không tìm thấy thông tin định danh người dùng" });

                int userId = int.Parse(userIdClaim);
                var notis = await _notiService.GetUserNotificationsAsync(userId);
                
                var alertData = notis.Take(limit).Select(n => new {
                    id = n.NotiId,
                    sensorName = n.Rule?.Sensor?.Name ?? "Unknown Sensor",
                    location = $"{n.Rule?.Sensor?.Hub?.Site?.Name} - {n.Rule?.Sensor?.Hub?.Name}",
                    value = ExtractValueFromMessage(n.Message),
                    metricUnit = n.Rule?.Sensor?.Type?.Unit ?? "",
                    severity = n.Rule?.Priority ?? "Info",
                    status = "Active",
                    time = n.SentAt
                }).ToList();

                return Ok(new { data = alertData });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách cảnh báo: " + ex.Message });
            }
        }

        private double? ExtractValueFromMessage(string? message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            try
            {
                // Format: "... (Value: 45.5 > Max: 40)" hoặc "... (Value: -5.2 < Min: 0)"
                var parts = message.Split(new[] { "Value: ", " >", " <", ")" }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    if (double.TryParse(parts[1], out double val)) return val;
                }
            }
            catch { }
            return null;
        }
    }
}
