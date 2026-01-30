using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;

namespace SWD.API.Controllers
{
    [Route("api/site")]
    [ApiController]
    [Authorize]
    public class SiteController : ControllerBase
    {
        private readonly ISiteService _siteService;

        public SiteController(ISiteService siteService)
        {
            _siteService = siteService;
        }

        /// <summary>
        /// Get All Sites
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllSitesAsync([FromQuery] string? search = null)
        {
            try
            {
                var sites = await _siteService.GetAllSitesAsync();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    sites = sites.Where(s =>
                        s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        s.SiteId.ToString().Contains(search)
                    ).ToList();
                }

                var siteDtos = sites.Select(s => new SiteDto
                {
                    SiteId = s.SiteId,
                    OrgId = s.OrgId,
                    OrgName = s.Org?.Name ?? "Unknown",
                    Name = s.Name,
                    Address = s.Address,
                    GeoLocation = s.GeoLocation,
                    HubCount = s.Hubs?.Count ?? 0
                }).ToList();

                return Ok(new
                {
                    message = "Lấy danh sách địa điểm thành công",
                    count = siteDtos.Count,
                    data = siteDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách địa điểm: " + ex.Message });
            }
        }

        /// <summary>
        /// Create Site - Create a new site
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> CreateSiteAsync([FromBody] CreateSiteDto request)
        {
            try
            {
                // Validate site name
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên địa điểm không được để trống" });

                if (request.Name.Length < 2)
                    return BadRequest(new { message = "Tên địa điểm phải có ít nhất 2 ký tự" });

                // Validate OrgId
                if (request.OrgId <= 0)
                    return BadRequest(new { message = "OrgId không hợp lệ. Địa điểm phải thuộc một tổ chức" });

                var site = new Site
                {
                    OrgId = request.OrgId,
                    Name = request.Name,
                    Address = request.Address,
                    GeoLocation = request.GeoLocation
                };

                await _siteService.CreateSiteAsync(site);

                return Ok(new
                {
                    message = "Tạo địa điểm thành công",
                    data = new SiteDto
                    {
                        SiteId = site.SiteId,
                        OrgId = site.OrgId,
                        Name = site.Name,
                        Address = site.Address,
                        GeoLocation = site.GeoLocation,
                        HubCount = 0
                    }
                });
            }
            catch (Exception ex)
            {
                // Handle foreign key constraint
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK_"))
                {
                    if (ex.Message.Contains("OrgId"))
                        return BadRequest(new { message = "OrgId không tồn tại trong hệ thống. Vui lòng chọn tổ chức hợp lệ" });
                }

                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique"))
                    return BadRequest(new { message = "Tên địa điểm đã tồn tại trong tổ chức này. Vui lòng sử dụng tên khác" });

                return BadRequest(new { message = "Lỗi khi tạo địa điểm: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Site By ID - Helper endpoint for CreatedAtAction
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSiteByIdAsync(int id)
        {
            try
            {
                var site = await _siteService.GetSiteByIdAsync(id);
                if (site == null)
                    return NotFound(new { message = "Không tìm thấy địa điểm với ID: " + id });

                var siteDto = new SiteDto
                {
                    SiteId = site.SiteId,
                    OrgId = site.OrgId,
                    OrgName = site.Org?.Name ?? "Unknown",
                    Name = site.Name,
                    Address = site.Address,
                    GeoLocation = site.GeoLocation,
                    HubCount = site.Hubs?.Count ?? 0
                };

                return Ok(new { message = "Lấy thông tin địa điểm thành công", data = siteDto });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thông tin địa điểm: " + ex.Message });
            }
        }

        /// <summary>
        /// Update Site - Update an existing site
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> UpdateSiteAsync(int id, [FromBody] CreateSiteDto request)
        {
            try
            {
                // Validate site ID
                if (id <= 0)
                    return BadRequest(new { message = "SiteId không hợp lệ" });

                var existingSite = await _siteService.GetSiteByIdAsync(id);
                if (existingSite == null)
                    return NotFound(new { message = "Không tìm thấy địa điểm với ID: " + id });

                // Validate name
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên địa điểm không được để trống" });

                if (request.Name.Length < 2)
                    return BadRequest(new { message = "Tên địa điểm phải có ít nhất 2 ký tự" });

                // Validate OrgId
                if (request.OrgId <= 0)
                    return BadRequest(new { message = "OrgId không hợp lệ" });

                existingSite.OrgId = request.OrgId;
                existingSite.Name = request.Name;
                existingSite.Address = request.Address;
                existingSite.GeoLocation = request.GeoLocation;

                await _siteService.UpdateSiteAsync(existingSite);

                return Ok(new { 
                    message = "Cập nhật địa điểm thành công", 
                    siteId = existingSite.SiteId,
                    name = existingSite.Name
                });
            }
            catch (Exception ex)
            {
                // Handle foreign key constraint
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK_"))
                {
                    if (ex.Message.Contains("OrgId"))
                        return BadRequest(new { message = "OrgId không tồn tại trong hệ thống. Vui lòng chọn tổ chức hợp lệ" });
                }

                return BadRequest(new { message = "Lỗi khi cập nhật địa điểm: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete Site - Delete a site
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> DeleteSiteAsync(int id)
        {
            try
            {
                // Validate site ID
                if (id <= 0)
                    return BadRequest(new { message = "SiteId không hợp lệ" });

                var existingSite = await _siteService.GetSiteByIdAsync(id);
                if (existingSite == null)
                    return NotFound(new { message = "Không tìm thấy địa điểm với ID: " + id });

                // Check if site has hubs
                var hubCount = existingSite.Hubs?.Count ?? 0;
                if (hubCount > 0)
                    return BadRequest(new { 
                        message = $"Không thể xóa địa điểm này vì còn {hubCount} Hub đang hoạt động. Vui lòng xóa hoặc chuyển các Hub trước",
                        hubCount = hubCount
                    });

                await _siteService.DeleteSiteAsync(id);

                return Ok(new { 
                    message = "Xóa địa điểm thành công", 
                    siteId = id,
                    name = existingSite.Name
                });
            }
            catch (Exception ex)
            {
                // Handle constraint violations
                if (ex.Message.Contains("constraint") || ex.Message.Contains("REFERENCE"))
                    return BadRequest(new { message = "Không thể xóa địa điểm này vì còn dữ liệu liên quan (Hub, user, v.v.). Vui lòng xóa dữ liệu liên quan trước" });

                return BadRequest(new { message = "Lỗi khi xóa địa điểm: " + ex.Message });
            }
        }
    }
}
