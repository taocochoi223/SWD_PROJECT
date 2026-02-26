using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace SWD.API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notiService;

        public NotificationController(INotificationService notiService)
        {
            _notiService = notiService;
        }

        /// <summary>
        /// Get User Notifications
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotificationsAsync(int userId)
        {
            try
            {
                // Validate userId
                if (userId <= 0)
                    return BadRequest(new { message = "UserId không hợp lệ" });

                var notis = await _notiService.GetUserNotificationsAsync(userId);

                var notiDtos = notis.Select(n => new NotificationDto
                {
                    Id = n.NotiId,
                    RuleId = n.RuleId,
                    UserId = n.UserId,
                    Message = n.Message,
                    SensorName = n.Rule?.Sensor?.Name,
                    Location = $"{n.Rule?.Sensor?.Hub?.Site?.Name} - {n.Rule?.Sensor?.Hub?.Name}",
                    Value = ExtractValueFromMessage(n.Message),
                    MetricUnit = n.Rule?.Sensor?.Type?.Unit,
                    Severity = n.Rule?.Priority,
                    Status = "Active",
                    Time = n.SentAt,
                    IsRead = n.IsRead,
                    SensorId = n.Rule?.SensorId
                }).ToList();

                var unreadCount = notiDtos.Count(n => n.IsRead == false);

                return Ok(new
                {
                    message = notiDtos.Count > 0 
                        ? "Lấy danh sách thông báo thành công" 
                        : "Người dùng chưa có thông báo nào",
                    userId = userId,
                    count = notiDtos.Count,
                    unreadCount = unreadCount,
                    data = notiDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách thông báo: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Unread Notifications Count
        /// </summary>
        [HttpGet("user/{userId}/unread-count")]
        public async Task<IActionResult> GetUnreadCountAsync(int userId)
        {
            try
            {
                // Validate userId
                if (userId <= 0)
                    return BadRequest(new { message = "UserId không hợp lệ" });

                var notis = await _notiService.GetUserNotificationsAsync(userId);
                var unreadCount = notis.Count(n => n.IsRead == false);

                return Ok(new 
                { 
                    message = "Lấy số thông báo chưa đọc thành công",
                    userId = userId,
                    unread_count = unreadCount,
                    total_count = notis.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy số thông báo: " + ex.Message });
            }
        }

        /// <summary>
        /// Mark Notification as Read
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsReadAsync(long id)
        {
            try
            {
                await _notiService.MarkAsReadAsync(id);
                return Ok(new { message = "Đánh dấu thông báo đã đọc thành công", id = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi đánh dấu thông báo: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Notification History with Paging and Filtering
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryAsync(
            [FromQuery] int? userId = null,
            [FromQuery] int? siteId = null,
            [FromQuery] int? sensorId = null,
            [FromQuery] string? severity = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Role-based Access Control (RBAC)
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToUpper();
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                  ?? User.FindFirst("UserId")?.Value;
                var userSiteIdClaim = User.FindFirst("SiteId")?.Value;
                
                if (userRole == "ADMIN")
                {
                    // Admin can view everything, use siteId and userId from query if provided
                }
                else if (userRole == "MANAGER" || userRole == "STAFF")
                {
                    // Staff/Manager can only see history of their assigned Site
                    if (string.IsNullOrEmpty(userSiteIdClaim) || !int.TryParse(userSiteIdClaim, out int assignedSiteId))
                    {
                        return Ok(new { message = "Bạn chưa được gán vào khu vực nào", data = new List<NotificationDto>(), totalCount = 0 });
                    }
                    
                    // Force their assigned site
                    siteId = assignedSiteId;

                    // To avoid seeing duplicate alerts (since one alert creates multiple notifications for all site users),
                    // we default to showing only notifications sent to the current user.
                    // If they specifically want to see notifications for another user in THEIR site, they can still pass userId.
                    if (!userId.HasValue)
                    {
                        userId = int.Parse(userIdClaim);
                    }
                }
                else 
                {
                    // Other roles (e.g. USER) only see their own notifications
                    if (string.IsNullOrEmpty(userIdClaim))
                        return Unauthorized(new { message = "Không tìm thấy thông tin định danh người dùng" });
                    
                    userId = int.Parse(userIdClaim);
                }

                var (items, totalCount) = await _notiService.GetNotificationsHistoryAsync(
                    userId, siteId, sensorId, severity, from, to, page, pageSize);

                var notiDtos = items.Select(n => new NotificationDto
                {
                    Id = n.NotiId,
                    RuleId = n.RuleId,
                    UserId = n.UserId,
                    Message = n.Message,
                    SensorName = n.Rule?.Sensor?.Name,
                    Location = $"{n.Rule?.Sensor?.Hub?.Site?.Name} - {n.Rule?.Sensor?.Hub?.Name}",
                    Value = ExtractValueFromMessage(n.Message),
                    MetricUnit = n.Rule?.Sensor?.Type?.Unit,
                    Severity = n.Rule?.Priority,
                    Status = "Active",
                    Time = n.SentAt,
                    IsRead = n.IsRead,
                    SensorId = n.Rule?.SensorId
                }).ToList();

                return Ok(new
                {
                    message = "Lấy lịch sử cảnh báo thành công",
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    data = notiDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy lịch sử cảnh báo: " + ex.Message });
            }
        }

        private double? ExtractValueFromMessage(string? message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            try
            {
                // Format: "... (Value: 45.5 > Max: 40)"
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