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
                // RBAC: Only owner or ADMIN can see specific user's notifications
                var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                         ?? User.FindFirst("UserId")?.Value;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToUpper();

                if (userRole != "ADMIN" && currentUserIdClaim != userId.ToString())
                {
                    return Forbid("Bạn không có quyền xem thông báo của người khác");
                }

                if (userId <= 0)
                    return BadRequest(new { message = "UserId không hợp lệ" });

                var notis = await _notiService.GetUserNotificationsAsync(userId);
                
                var notiDtos = notis.Select(n => new NotificationDto
                {
                    Id = n.NotiId,
                    RuleId = n.RuleId,
                    UserId = n.UserId,
                    OrgId = n.OrgId,
                    Message = n.Message,
                    Location = $"{n.Rule?.Hub?.Site?.Name} - {n.Rule?.Hub?.Name}",
                    Value = ExtractValueFromMessage(n.Message),
                    Severity = n.Rule?.Priority,
                    Status = "Active",
                    Time = n.SentAt,
                    IsRead = n.IsRead
                }).ToList();

                var unreadCount = notiDtos.Count(n => n.IsRead == false);

                return Ok(new
                {
                    message = notiDtos.Count > 0 
                        ? "Lấy danh sách thông báo thành công" 
                        : "Thông báo trống",
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
                // RBAC: Only owner or ADMIN can see specific user's unread count
                var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                         ?? User.FindFirst("UserId")?.Value;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToUpper();

                if (userRole != "ADMIN" && currentUserIdClaim != userId.ToString())
                {
                    return Forbid();
                }

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
                // RBAC: Pass current userId to ensure user only marks their own notification as read
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToUpper();
                var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                         ?? User.FindFirst("UserId")?.Value;

                int? currentUserId = null;
                if (userRole != "ADMIN" && int.TryParse(currentUserIdClaim, out int parsedId))
                {
                    currentUserId = parsedId;
                }
                
                await _notiService.MarkAsReadAsync(id, currentUserId);
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
        /// <param name="sortBy">Sắp xếp theo field: sentAt | severity | isRead (default: sentAt)</param>
        /// <param name="sortOrder">Thứ tự sắp xếp: asc | desc (default: desc)</param>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryAsync(
            [FromQuery] int? userId = null,
            [FromQuery] int? siteId = null,
            [FromQuery] int? hubId = null,
            [FromQuery] string? severity = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                // Role-based Access Control (RBAC)
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToUpper();
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                  ?? User.FindFirst("UserId")?.Value;
                var userSiteIdClaim = User.FindFirst("SiteId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Không tìm thấy thông tin định danh người dùng" });

                int currentUserId = int.Parse(userIdClaim);
                
                if (userRole == "ADMIN")
                {
                    // Admin can view everything, use siteId and userId from query if provided
                    // No overrides needed
                }
                else if (userRole == "MANAGER" || userRole == "STAFF")
                {
                    // Nếu đã có gán khu vực (SiteId) -> Ép lọc theo khu vực đó
                    if (!string.IsNullOrEmpty(userSiteIdClaim) && int.TryParse(userSiteIdClaim, out int assignedSiteId))
                    {
                        siteId = assignedSiteId;

                        // Security: STAFF can ONLY see their own notifications within their site
                        if (userRole == "STAFF")
                        {
                            userId = currentUserId;
                        }
                        // MANAGER can see others in their site if they pass userId, otherwise own by default
                        else if (!userId.HasValue)
                        {
                            userId = currentUserId;
                        }
                    }
                    else
                    {
                        // FALLBACK: SiteId NULL hoặc Rỗng -> Xem ĐƯỢC HẾT (Globally)
                        // Giữ nguyên siteId và userId từ Query (có thể null để xem tất cả)
                    }
                }
                else 
                {
                    // Other roles (Regular Users): Only see their own notifications
                    userId = currentUserId;
                    siteId = null;
                }

                var (items, totalCount) = await _notiService.GetNotificationsHistoryAsync(
                    userId, siteId, hubId, severity, from, to, page, pageSize, sortBy, sortOrder);

                var notiDtos = items.Select(n => new NotificationDto
                {
                    Id = n.NotiId,
                    RuleId = n.RuleId,
                    UserId = n.UserId,
                    OrgId = n.OrgId,
                    Message = n.Message,
                    Location = $"{n.Rule?.Hub?.Site?.Name} - {n.Rule?.Hub?.Name}",
                    Value = ExtractValueFromMessage(n.Message),
                    Severity = n.Rule?.Priority,
                    Status = "Active",
                    Time = n.SentAt,
                    IsRead = n.IsRead
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