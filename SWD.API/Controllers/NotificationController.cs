using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;

namespace SWD.API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
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
                var notis = await _notiService.GetUserNotificationsAsync(userId);

                var notiDtos = notis.Select(n => new NotificationDto
                {
                    NotiId = n.NotiId,
                    HistoryId = n.HistoryId,
                    UserId = n.UserId,
                    Message = n.Message,
                    SentAt = n.SentAt,
                    IsRead = n.IsRead,
                    SensorId = n.History?.SensorId,
                    SensorName = n.History?.Sensor?.Name,
                    Severity = n.History?.Severity,
                    TriggeredAt = n.History?.TriggeredAt
                }).ToList();

                return Ok(new
                {
                    message = "Lấy danh sách thông báo thành công",
                    count = notiDtos.Count,
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
                var notis = await _notiService.GetUserNotificationsAsync(userId);
                var unreadCount = notis.Count(n => n.IsRead == false);

                return Ok(new 
                { 
                    message = "Lấy số thông báo chưa đọc thành công",
                    unread_count = unreadCount 
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