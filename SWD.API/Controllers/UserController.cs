using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using BCrypt.Net;

namespace SWD.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public UserController(IUserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        /// <summary>
        /// Create User Account - Admin creates account for a new user
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> CreateUserAccount([FromBody] RegisterUserDto request)
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email không được để trống" });

            // Email format validation
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest(new { message = "Email không đúng định dạng. VD: example@domain.com" });

            // Validate full name
            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(new { message = "Tên người dùng không được để trống" });

            if (request.FullName.Length < 2)
                return BadRequest(new { message = "Tên người dùng phải có ít nhất 2 ký tự" });

            // Validate RoleId
            if (request.RoleId <= 0)
                return BadRequest(new { message = "RoleId không hợp lệ. Vui lòng chọn vai trò cho người dùng" });

            // Validate OrgId
            if (request.OrgId <= 0)
                return BadRequest(new { message = "OrgId không hợp lệ. Người dùng phải thuộc một tổ chức" });

            string randomPassword = Guid.NewGuid().ToString().Substring(0, 8) + "@A1";
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(randomPassword);

            var newUser = new User
            {
                OrgId = request.OrgId,
                SiteId = (request.SiteId == 0) ? null : request.SiteId,
                FullName = request.FullName ?? string.Empty,
                Email = request.Email ?? string.Empty,
                PasswordHash = hashedPassword,
                RoleId = request.RoleId,
                IsActive = true 
            };

            try
            {
                await _userService.RegisterUserAsync(newUser);

                // Fire-and-forget email sending (không đợi email xong)
                string subject = "Welcome to WinMart IoT System";
                string body = $"<h3>Xin chào {request.FullName},</h3>" +
                              $"<p>Tài khoản của bạn đã được tạo thành công.</p>" +
                              $"<p>Email: <b>{request.Email}</b></p>" +
                              $"<p>Mật khẩu tạm thời: <b>{randomPassword}</b></p>" +
                              $"<p><strong>Lưu ý:</strong> Vui lòng đăng nhập và đổi mật khẩu ngay để bảo mật tài khoản.</p>";

                // Gửi email trong background, không block response
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendEmailAsync(request.Email!, subject, body);
                    }
                    catch (Exception emailEx)
                    {
                        // Log error nhưng không ảnh hưởng response
                        Console.WriteLine($"Email sending failed: {emailEx.Message}");
                    }
                });

                // Trả response ngay lập tức
                return Ok(new
                {
                    message = "Tạo tài khoản thành công! Email chứa mật khẩu tạm thời sẽ được gửi trong giây lát",
                    userId = newUser.UserId,
                    email = newUser.Email,
                    fullName = newUser.FullName,
                });
            }
            catch (Exception ex)
            {
                // Handle specific errors
                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique") || ex.Message.Contains("exists"))
                    return BadRequest(new { message = "Email này đã được sử dụng. Vui lòng sử dụng email khác" });
                
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK_"))
                {
                    if (ex.Message.Contains("RoleId"))
                        return BadRequest(new { message = "RoleId không tồn tại trong hệ thống. Vui lòng chọn vai trò hợp lệ" });
                    if (ex.Message.Contains("OrgId"))
                        return BadRequest(new { message = "OrgId không tồn tại trong hệ thống. Vui lòng chọn tổ chức hợp lệ" });
                    if (ex.Message.Contains("SiteId"))
                        return BadRequest(new { message = "SiteId không tồn tại trong hệ thống. Vui lòng chọn địa điểm hợp lệ" });
                }
                
                return BadRequest(new { message = "Lỗi khi tạo tài khoản: " + ex.Message });
            }
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
                // Validate user ID
                if (id <= 0)
                    return BadRequest(new { message = "UserId không hợp lệ" });

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng với ID: " + id });

                // Track what was updated
                bool hasChanges = false;

                // Update full name
                if (!string.IsNullOrEmpty(request.FullName))
                {
                    if (request.FullName.Length < 2)
                        return BadRequest(new { message = "Tên người dùng phải có ít nhất 2 ký tự" });
                    
                    user.FullName = request.FullName;
                    hasChanges = true;
                }
                
                // Update SiteId
                if (request.SiteId.HasValue)
                {
                    if (request.SiteId.Value < 0)
                        return BadRequest(new { message = "SiteId không hợp lệ" });
                    
                    user.SiteId = request.SiteId.Value;
                    hasChanges = true;
                }

                if (!hasChanges)
                    return BadRequest(new { message = "Không có thông tin nào được cập nhật" });

                await _userService.UpdateUserAsync(user);

                return Ok(new 
                { 
                    message = "Cập nhật người dùng thành công", 
                    userId = user.UserId,
                    fullName = user.FullName,
                    siteId = user.SiteId
                });
            }
            catch (Exception ex)
            {
                // Handle foreign key constraint errors
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK_"))
                {
                    if (ex.Message.Contains("SiteId"))
                        return BadRequest(new { message = "SiteId không tồn tại trong hệ thống. Vui lòng chọn địa điểm hợp lệ" });
                }

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
                // Validate user ID
                if (id <= 0)
                    return BadRequest(new { message = "UserId không hợp lệ" });

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

        /// <summary>
        /// Deactivate User
        /// </summary>
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> DeactivateUserAsync(int id)
        {
            try
            {
                // Validate user ID
                if (id <= 0)
                    return BadRequest(new { message = "UserId không hợp lệ" });

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng với ID: " + id });

                if (user.IsActive == false)
                    return BadRequest(new { message = "Tài khoản này đã bị vô hiệu hóa trước đó" });

                await _userService.DeactivateUserAsync(id);

                return Ok(new { 
                    message = "Vô hiệu hóa tài khoản thành công", 
                    userId = id,
                    email = user.Email,
                    name = user.FullName
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi vô hiệu hóa tài khoản: " + ex.Message });
            }
        }
    }

}
