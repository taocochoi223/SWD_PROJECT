using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;

namespace SWD.API.Controllers
{
    [Route("api/hubs")]
    [ApiController]
    [Authorize]
    public class HubController : ControllerBase
    {
        private readonly IHubService _hubService;

        public HubController(IHubService hubService)
        {
            _hubService = hubService;
        }
        /// <summary>Get all hubs</summary>
        /// <param name="search">Tìm kiếm theo tên hoặc địa chỉ MAC</param>
        /// <param name="isOnline">Lọc theo trạng thái online/offline</param>
        /// <param name="pageNumber">Số trang (bắt đầu từ 1). Chỉ phân trang khi truyền cả pageNumber và pageSize</param>
        /// <param name="pageSize">Số lượng mỗi trang. Chỉ phân trang khi truyền cả pageNumber và pageSize</param>
        /// <param name="sortBy">Sắp xếp theo field: name | macAddress | isOnline | lastHandshake (default: hubId)</param>
        /// <param name="sortOrder">Thứ tự sắp xếp: asc | desc (default: asc)</param>
        [HttpGet]

        public async Task<IActionResult> GetAllHubsAsync(
            [FromQuery] string? search = null,
            [FromQuery] bool? isOnline = null,
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = "asc")
        {
            try
            {
                if (pageNumber.HasValue && pageNumber.Value < 1)
                    return BadRequest(new { message = "pageNumber phải lớn hơn hoặc bằng 1" });
                if (pageSize.HasValue && pageSize.Value < 1)
                    return BadRequest(new { message = "pageSize phải lớn hơn hoặc bằng 1" });

                var siteIdClaim = User.FindFirst("SiteId")?.Value;
                int? userSiteId = !string.IsNullOrEmpty(siteIdClaim) ? int.Parse(siteIdClaim) : null;

                var (hubs, totalCount) = await _hubService.GetAllHubsAsync(search, isOnline, userSiteId, pageNumber, pageSize, sortBy, sortOrder);

                int? totalPages = (pageNumber.HasValue && pageSize.HasValue)
                    ? (int)Math.Ceiling((double)totalCount / pageSize.Value)
                    : null;

                var hubDtos = hubs.Select(h => new HubDto
                {
                    HubId = h.HubId,
                    Name = h.Name,
                    MacAddress = h.MacAddress,
                    IsOnline = h.IsOnline,
                    LastHandshake = h.LastHandshake,
                    SiteId = h.SiteId,
                    SiteName = h.Site?.Name ?? "Unassigned",
                    SensorCount = h.Sensors?.Count ?? 0
                }).ToList();

                return Ok(new
                {
                    message = "Lấy danh sách Hub thành công",
                    totalCount,
                    pageNumber,
                    pageSize,
                    totalPages,
                    data = hubDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách Hub: " + ex.Message });
            }
        }

        /// <summary>
        /// Get hub by id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHubByIdAsync(int id)
        {
            try
            {
                var hub = await _hubService.GetHubByIdAsync(id);
                if (hub == null)
                    return NotFound(new { message = "Không tìm thấy Hub với ID: " + id });

                var siteIdClaim = User.FindFirst("SiteId")?.Value;
                int? userSiteId = !string.IsNullOrEmpty(siteIdClaim) ? int.Parse(siteIdClaim) : null;

                if (userSiteId.HasValue && hub.SiteId != userSiteId.Value)
                {
                    return StatusCode(403, new { message = "Bạn không có quyền truy cập Hub này" });
                }

                var hubDto = new HubDto
                {
                    HubId = hub.HubId,
                    Name = hub.Name,
                    MacAddress = hub.MacAddress,
                    IsOnline = hub.IsOnline,
                    LastHandshake = hub.LastHandshake,
                    SiteId = hub.SiteId,
                    SiteName = hub.Site?.Name ?? "Unassigned",
                    SensorCount = hub.Sensors?.Count ?? 0
                };
                return Ok(new
                {
                    message = "Lấy thông tin Hub thành công",
                    data = hubDto
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thông tin Hub: " + ex.Message });
            }
        }

        /// <summary>
        /// Create hub
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> CreateHubAsync([FromBody] RegisterHubDto request)
        {
            try
            {
                // Validate hub name
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên Hub không được để trống" });

                if (request.Name.Length < 2)
                    return BadRequest(new { message = "Tên Hub phải có ít nhất 2 ký tự" });

                // Validate MAC address
                if (string.IsNullOrWhiteSpace(request.MacAddress))
                    return BadRequest(new { message = "Địa chỉ MAC không được để trống" });

            
                // Validate SiteId
                if (request.SiteId <= 0)
                    return BadRequest(new { message = "SiteId không hợp lệ. Vui lòng chọn địa điểm cho Hub" });

                // Check for duplicate MAC address
                var ex = await _hubService.GetHubByMacAsync(request.MacAddress);
                if (ex != null)
                {
                    return BadRequest(new { 
                        message = "Hub với địa chỉ MAC này đã tồn tại",
                        existingHubId = ex.HubId,
                        existingHubName = ex.Name
                    });
                }

                var hub = new Hub
                {
                    SiteId = request.SiteId,
                    Name = request.Name,
                    MacAddress = request.MacAddress,
                    IsOnline = false 
                };

                await _hubService.CreateHubAsync(hub);

                return Ok(new
                {
                    message = "Tạo Hub thành công",
                    hub = new HubDto
                    {
                        HubId = hub.HubId,
                        Name = hub.Name,
                        MacAddress = hub.MacAddress,
                        IsOnline = hub.IsOnline,
                        SiteId = hub.SiteId,
                        SensorCount = 0
                    }
                });
            }
            catch (Exception ex)
            {
                // Handle foreign key constraint
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK_"))
                {
                    if (ex.Message.Contains("SiteId"))
                        return BadRequest(new { message = "SiteId không tồn tại trong hệ thống. Vui lòng chọn địa điểm hợp lệ" });
                }

                return BadRequest(new { message = "Lỗi khi tạo Hub: " + ex.Message });
            }
        }

        /// <summary>
        /// Update hub
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> UpdateHubAsync(int id, [FromBody] UpdateHubDto request)
        {
            try
            {
                // Validate hub ID
                if (id <= 0)
                    return BadRequest(new { message = "HubId không hợp lệ" });

                var existingHub = await _hubService.GetHubByIdAsync(id);
                if (existingHub == null)
                    return NotFound(new { message = "Không tìm thấy Hub với ID: " + id });

                // Update SiteId
                if (request.SiteId.HasValue)
                {
                    if (request.SiteId.Value <= 0)
                        return BadRequest(new { message = "SiteId không hợp lệ" });
                    
                    existingHub.SiteId = request.SiteId.Value;
                }

                // Update Name
                if (!string.IsNullOrEmpty(request.Name))
                {
                    if (request.Name.Length < 2)
                        return BadRequest(new { message = "Tên Hub phải có ít nhất 2 ký tự" });
                    
                    existingHub.Name = request.Name;
                }

                // Update MAC address
                if (!string.IsNullOrEmpty(request.MacAddress) && request.MacAddress != existingHub.MacAddress)
                {
                    // Validate MAC format
                    var macPattern = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$";
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.MacAddress, macPattern))
                        return BadRequest(new { message = "Địa chễ MAC không đúng định dạng. VD: AA:BB:CC:DD:EE:FF hoặc AA-BB-CC-DD-EE-FF" });

                    // Check for duplicate
                    var duplicateHub = await _hubService.GetHubByMacAsync(request.MacAddress);
                    if (duplicateHub != null && duplicateHub.HubId != id)
                    {
                        return BadRequest(new { 
                            message = "Hub với địa chỉ MAC này đã tồn tại",
                            existingHubId = duplicateHub.HubId,
                            existingHubName = duplicateHub.Name
                        });
                    }
                    existingHub.MacAddress = request.MacAddress;
                }

                await _hubService.UpdateHubAsync(existingHub);
                return Ok(new 
                { 
                    message = "Cập nhật Hub thành công", 
                    hubId = existingHub.HubId,
                    name = existingHub.Name,
                    macAddress = existingHub.MacAddress,
                    siteId = existingHub.SiteId
                });
            }
            catch (Exception ex)
            {
                // Handle foreign key constraint
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK_"))
                {
                    if (ex.Message.Contains("SiteId"))
                        return BadRequest(new { message = "SiteId không tồn tại trong hệ thống. Vui lòng chọn địa điểm hợp lệ" });
                }

                return BadRequest(new { message = "Lỗi khi cập nhật Hub: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete Hub - Delete a hub
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> DeleteHubAsync(int id)
        {
            try
            {
                // Validate hub ID
                if (id <= 0)
                    return BadRequest(new { message = "HubId không hợp lệ" });

                var existingHub = await _hubService.GetHubByIdAsync(id);
                if (existingHub == null)
                    return NotFound(new { message = "Không tìm thấy Hub với ID: " + id });

                // Check if hub has sensors
                var sensorCount = existingHub.Sensors?.Count ?? 0;
                if (sensorCount > 0)
                    return BadRequest(new { 
                        message = $"Không thể xóa Hub này vì còn {sensorCount} cảm biến đang hoạt động. Vui lòng xóa hoặc chuyển các cảm biến trước",
                        sensorCount = sensorCount
                    });

                await _hubService.DeleteHubAsync(id);

                return Ok(new 
                { 
                    message = "Xóa Hub thành công", 
                    hubId = id,
                    name = existingHub.Name
                });
            }
            catch (Exception ex)
            {
                // Handle constraint violations
                if (ex.Message.Contains("constraint") || ex.Message.Contains("REFERENCE"))
                    return BadRequest(new { message = "Không thể xóa Hub này vì còn dữ liệu liên quan (cảm biến, alert, v.v.). Vui lòng xóa dữ liệu liên quan trước" });

                return BadRequest(new { message = "Lỗi khi xóa Hub: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Hub Readings - Get all readings for a hub
        /// </summary>
        [HttpGet("{id}/readings")]
        public async Task<IActionResult> GetHubReadingsAsync(int id, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            try
            {
                var hub = await _hubService.GetHubWithReadingsAsync(id, from, to);

                if (hub == null)
                    return NotFound(new { message = "Không tìm thấy Hub với ID: " + id });

                // KIỂM TRA PHÂN QUYỀN
                var siteIdClaim = User.FindFirst("SiteId")?.Value;
                int? userSiteId = !string.IsNullOrEmpty(siteIdClaim) ? int.Parse(siteIdClaim) : null;

                if (userSiteId.HasValue && hub.SiteId != userSiteId.Value)
                {
                    return StatusCode(403, new { message = "Bạn không có quyền truy cập Hub này" });
                }

                var result = new HubReadingsDto
                {
                    HubId = hub.HubId,
                    Name = hub.Name,
                    MacAddress = hub.MacAddress,
                    Sensors = hub.Sensors?.Select(s => new SensorReadingDto
                    {
                        SensorId = s.SensorId,
                        Name = s.Name,
                        TypeName = s.Type?.TypeName ?? "Unknown",
                        Unit = s.Type?.Unit ?? "",
                        // Hub level readings
                        Readings = hub.SensorDatas?.Select(r => new ReadingValueDto
                        {
                            RecordedAt = r.RecordedAt ?? DateTime.MinValue,
                            Value = ExtractValueFromJson(r.JsonValue, s.Type?.TypeName)
                        }).OrderByDescending(r => r.RecordedAt).ToList() ?? new List<ReadingValueDto>()
                    }).ToList() ?? new List<SensorReadingDto>()
                };

                return Ok(new
                {
                    message = "Lấy dữ liệu đo của Hub thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu đo của Hub: " + ex.Message });
            }
        }

        private float ExtractValueFromJson(string? json, string? typeName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(typeName)) return 0;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (typeName.Contains("Temperature") || typeName.Contains("Nhiệt độ"))
                {
                    if (root.TryGetProperty("v1", out var v)) return (float)v.GetDouble();
                }
                else if (typeName.Contains("Humidity") || typeName.Contains("Độ ẩm"))
                {
                    if (root.TryGetProperty("v2", out var v)) return (float)v.GetDouble();
                }
                else if (typeName.Contains("Pressure") || typeName.Contains("Áp suất"))
                {
                    if (root.TryGetProperty("v3", out var v)) return (float)v.GetDouble();
                }
            }
            catch { }
            return 0;
        }
    }
}
