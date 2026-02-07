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
                    NotiId = n.NotiId,
                    RuleId = n.RuleId,
                    UserId = n.UserId,
                    Message = n.Message,
                    SentAt = n.SentAt,
                    IsRead = n.IsRead,
                    SensorId = n.Rule?.SensorId,
                    SensorName = n.Rule?.Sensor?.Name,
                    Severity = n.Rule?.Priority, // Map Priority to Severity
                    TriggeredAt = n.SentAt // Notification sent time is trigger time
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
    }
}