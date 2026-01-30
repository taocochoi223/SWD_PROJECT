using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SWD.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;

        public AuthController(IUserService userService, IJwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Login - Authenticate user and return JWT token
        /// </summary>
        [ HttpPost("login")]
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
    }
}