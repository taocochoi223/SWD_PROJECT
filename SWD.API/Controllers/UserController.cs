using Microsoft.AspNetCore.Authorization; // Add this
using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;

namespace SWD.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get All Users
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();

                var userDtos = users.Select(user => new
                {
                    userId = user.UserId,
                    email = user.Email,
                    fullName = user.FullName,
                    roleId = user.RoleId,
                    roleName = user.Role?.RoleName,
                    orgId = user.OrgId,
                    siteId = user.SiteId,
                    siteName = user.Site?.Name,
                    isActive = user.IsActive
                }).ToList();

                return Ok(new
                {
                    message = "Lấy danh sách người dùng thành công",
                    count = userDtos.Count,
                    data = userDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách người dùng: " + ex.Message });
            }
        }

        /// <summary>
        /// Get User By ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng với ID: " + id });

                return Ok(new
                {
                    message = "Lấy thông tin người dùng thành công",
                    userId = user.UserId,
                    email = user.Email,
                    fullName = user.FullName,
                    roleId = user.RoleId,
                    roleName = user.Role?.RoleName,
                    orgId = user.OrgId,
                    siteId = user.SiteId,
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thông tin người dùng: " + ex.Message });
            }
        }

        /// <summary>
        /// Update User
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> UpdateUserAsync(int id, [FromBody] UpdateUserDto request)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng với ID: " + id });

                // Update fields
                if (!string.IsNullOrEmpty(request.FullName))
                    user.FullName = request.FullName;
                
                if (request.SiteId.HasValue)
                    user.SiteId = request.SiteId.Value;

                await _userService.UpdateUserAsync(user);

                return Ok(new 
                { 
                    message = "Cập nhật người dùng thành công", 
                    userId = user.UserId,
                    fullName = user.FullName
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật người dùng: " + ex.Message });
            }
        }

        /// <summary>
        /// Activate User
        /// </summary>
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> ActivateUserAsync(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng với ID: " + id });

                if (user.IsActive == true)
                    return BadRequest(new { message = "Người dùng này đã được kích hoạt trước đó" });

                user.IsActive = true;
                await _userService.UpdateUserAsync(user);

                return Ok(new 
                { 
                    message = "Kích hoạt người dùng thành công", 
                    userId = id,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi kích hoạt người dùng: " + ex.Message });
            }
        }
    }

}
