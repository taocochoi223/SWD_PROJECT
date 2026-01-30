using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SWD.API.Controllers
{
    [Route("api/organizations")]
    [ApiController]
    [Authorize]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService _organizationService;

        public OrganizationController(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        /// <summary>
        /// Get all organizations
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> GetAllOrganizationsAsync()
        {
            try
            {
                var orgs = await _organizationService.GetAllOrganizationsAsync();
                var orgDtos = orgs.Select(o => new OrganizationDto
                {
                    OrgId = o.OrgId,
                    Name = o.Name,
                    Description = o.Description,
                    CreatedAt = o.CreatedAt,
                    SiteCount = o.Sites?.Count ?? 0
                }).ToList();

                return Ok(new
                {
                    message = "Lấy danh sách tổ chức thành công",
                    count = orgDtos.Count,
                    data = orgDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách tổ chức: " + ex.Message });
            }
        }

        /// <summary>
        /// Get organization by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> GetOrganizationByIdAsync(int id)
        {
            try
            {
                var org = await _organizationService.GetOrganizationByIdAsync(id);
                if (org == null)
                    return NotFound(new { message = "Không tìm thấy tổ chức với ID: " + id });

                var orgDto = new OrganizationDto
                {
                    OrgId = org.OrgId,
                    Name = org.Name,
                    Description = org.Description,
                    CreatedAt = org.CreatedAt,
                    SiteCount = org.Sites?.Count ?? 0
                };

                return Ok(new { message = "Lấy thông tin tổ chức thành công", data = orgDto });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thông tin tổ chức: " + ex.Message });
            }
        }

        /// <summary>
        /// Create a new organization
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> CreateOrganizationAsync([FromBody] CreateOrganizationDto request)
        {
            try
            {
                // Validate organization name
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên tổ chức không được để trống" });

                if (request.Name.Length < 2)
                    return BadRequest(new { message = "Tên tổ chức phải có ít nhất 2 ký tự" });

                // Validate description length
                if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 500)
                    return BadRequest(new { message = "Mô tả không được vượt quá 500 ký tự" });

                var org = new Organization
                {
                    Name = request.Name,
                    Description = request.Description
                };

                var createdOrg = await _organizationService.CreateOrganizationAsync(org);

                var orgDto = new OrganizationDto
                {
                    OrgId = createdOrg.OrgId,
                    Name = createdOrg.Name,
                    Description = createdOrg.Description,
                    CreatedAt = createdOrg.CreatedAt,
                    SiteCount = 0
                };

                return Ok(new { message = "Tạo tổ chức thành công", data = orgDto });
            }
            catch (Exception ex)
            {
                // Handle duplicate organization names
                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique"))
                    return BadRequest(new { message = "Tên tổ chức đã tồn tại. Vui lòng sử dụng tên khác" });

                return BadRequest(new { message = "Lỗi khi tạo tổ chức: " + ex.Message });
            }
        }

        /// <summary>
        /// Update an organization
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> UpdateOrganizationAsync(int id, [FromBody] UpdateOrganizationDto request)
        {
            try
            {
                // Validate organization ID
                if (id <= 0)
                    return BadRequest(new { message = "OrgId không hợp lệ" });

                var existingOrg = await _organizationService.GetOrganizationByIdAsync(id);
                if (existingOrg == null)
                    return NotFound(new { message = "Không tìm thấy tổ chức với ID: " + id });

                // Validate name
                if (!string.IsNullOrEmpty(request.Name))
                {
                    if (request.Name.Length < 2)
                        return BadRequest(new { message = "Tên tổ chức phải có ít nhất 2 ký tự" });
                    
                    existingOrg.Name = request.Name;
                }
                
                // Validate description
                if (request.Description != null)
                {
                    if (request.Description.Length > 500)
                        return BadRequest(new { message = "Mô tả không được vượt quá 500 ký tự" });
                    
                    existingOrg.Description = request.Description;
                }

                await _organizationService.UpdateOrganizationAsync(existingOrg);

                return Ok(new { 
                    message = "Cập nhật tổ chức thành công", 
                    orgId = id,
                    name = existingOrg.Name
                });
            }
            catch (Exception ex)
            {
                // Handle duplicate names
                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique"))
                    return BadRequest(new { message = "Tên tổ chức đã tồn tại. Vui lòng sử dụng tên khác" });

                return BadRequest(new { message = "Lỗi khi cập nhật tổ chức: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete an organization
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> DeleteOrganizationAsync(int id)
        {
            try
            {
                // Validate organization ID
                if (id <= 0)
                    return BadRequest(new { message = "OrgId không hợp lệ" });

                var existingOrg = await _organizationService.GetOrganizationByIdAsync(id);
                if (existingOrg == null)
                    return NotFound(new { message = "Không tìm thấy tổ chức với ID: " + id });

                // Check if organization has sites or users
                var siteCount = existingOrg.Sites?.Count ?? 0;
                if (siteCount > 0)
                    return BadRequest(new { 
                        message = $"Không thể xóa tổ chức này vì còn {siteCount} địa điểm đang sử dụng. Vui lòng xóa hoặc chuyển các địa điểm trước",
                        siteCount = siteCount
                    });

                await _organizationService.DeleteOrganizationAsync(id);

                return Ok(new { 
                    message = "Xóa tổ chức thành công", 
                    orgId = id,
                    name = existingOrg.Name
                });
            }
            catch (Exception ex)
            {
                // Handle constraint violations
                if (ex.Message.Contains("constraint") || ex.Message.Contains("REFERENCE"))
                    return BadRequest(new { message = "Không thể xóa tổ chức này vì còn dữ liệu liên quan (site, user, v.v.). Vui lòng xóa dữ liệu liên quan trước" });

                return BadRequest(new { message = "Lỗi khi xóa tổ chức: " + ex.Message });
            }
        }
    }
}
