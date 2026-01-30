using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using System;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SWD.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;

        public AuthController(IUserService userService, IEmailService emailService, IJwtService jwtService)
        {
            _userService = userService;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Login - Authenticate user and return JWT token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email và password không được để trống" });

            var user = await _userService.AuthenticateUserAsync(request.Email, request.Password);
            if (user == null) 
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });

            if (user.IsActive == false)
                return Unauthorized(new { message = "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên" });

            string jwtToken = _jwtService.GenerateToken(user);

            return Ok(new 
            { 
                message = "Đăng nhập thành công",
                token = jwtToken,
                user = new 
                { 
                    id = user.UserId.ToString(), 
                    email = user.Email,
                    name = user.FullName, 
                    role = user.Role?.RoleName ?? "USER"
                } 
            });
        }

        /// <summary>
        /// CreateAccount - Register a new user (Admin create for user)
        /// </summary>
        [HttpPost("createAccount")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email không được để trống" });

            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(new { message = "Tên người dùng không được để trống" });

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

                string subject = "Welcome to WinMart IoT System";
                string body = $"<h3>Xin chào {request.FullName},</h3>" +
                              $"<p>Tài khoản của bạn đã được tạo thành công.</p>" +
                              $"<p>Email: <b>{request.Email}</b></p>" +
                              $"<p>Mật khẩu tạm thời: <b>{randomPassword}</b></p>" +
                              $"<p><strong>Lưu ý:</strong> Vui lòng đăng nhập và đổi mật khẩu ngay để bảo mật tài khoản.</p>";

                try
                {
                    await _emailService.SendEmailAsync(request.Email!, subject, body);
                    return Ok(new { 
                        message = "Tạo tài khoản thành công! Email chứa mật khẩu tạm thời đã được gửi đến " + request.Email,
                        userId = newUser.UserId,
                        email = newUser.Email
                    });
                }
                catch (Exception emailEx)
                {
                    return Ok(new { 
                        message = "Tạo tài khoản thành công nhưng không thể gửi email. Vui lòng liên hệ quản trị viên để lấy mật khẩu",
                        warning = "Email sending failed: " + emailEx.Message,
                        userId = newUser.UserId,
                        email = newUser.Email
                    });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique") || ex.Message.Contains("exists"))
                    return BadRequest(new { message = "Email này đã được sử dụng. Vui lòng sử dụng email khác" });
                
                return BadRequest(new { message = "Lỗi khi tạo tài khoản: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Current User 
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Token không hợp lệ hoặc bị thiếu" });
                
                if (!int.TryParse(userIdClaim, out int userId))
                    return BadRequest(new { message = "Token chứa thông tin không hợp lệ" });

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy thông tin người dùng" });

                if (user.IsActive == false)
                    return Unauthorized(new { message = "Tài khoản đã bị vô hiệu hóa" });

                return Ok(new
                {
                    message = "Lấy thông tin người dùng thành công",
                    id = user.UserId.ToString(),
                    email = user.Email,
                    name = user.FullName,
                    role = user.Role?.RoleName ?? "USER",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thông tin người dùng: " + ex.Message });
            }
        }

        /// <summary>
        /// Deactivate User
        /// </summary>
        [HttpPost("deactivate/{userId}")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng với ID: " + userId });

                if (user.IsActive == false)
                    return BadRequest(new { message = "Tài khoản này đã bị vô hiệu hóa trước đó" });

                await _userService.DeactivateUserAsync(userId);

                return Ok(new { 
                    message = "Vô hiệu hóa tài khoản thành công", 
                    userId = userId,
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