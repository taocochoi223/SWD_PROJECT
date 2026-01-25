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
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên địa điểm không được để trống" });

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
                var existingSite = await _siteService.GetSiteByIdAsync(id);
                if (existingSite == null)
                    return NotFound(new { message = "Không tìm thấy địa điểm với ID: " + id });

                existingSite.OrgId = request.OrgId;
                existingSite.Name = request.Name;
                existingSite.Address = request.Address;
                existingSite.GeoLocation = request.GeoLocation;

                await _siteService.UpdateSiteAsync(existingSite);

                return Ok(new { message = "Cập nhật địa điểm thành công", siteId = existingSite.SiteId });
            }
            catch (Exception ex)
            {
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
                var existingSite = await _siteService.GetSiteByIdAsync(id);
                if (existingSite == null)
                    return NotFound(new { message = "Không tìm thấy địa điểm với ID: " + id });

                await _siteService.DeleteSiteAsync(id);

                return Ok(new { message = "Xóa địa điểm thành công", siteId = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xóa địa điểm: " + ex.Message });
            }
        }
    }
}
