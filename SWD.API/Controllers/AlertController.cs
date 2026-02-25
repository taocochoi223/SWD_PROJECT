using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;

namespace SWD.API.Controllers
{
    [Route("api/alerts")]
    [ApiController]
    [Authorize]
    public class AlertController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertController(IAlertService alertService)
        {
            _alertService = alertService;
        }



        /// <summary>
        /// Get Alert Rules - For configuration
        /// </summary>
        /// <param name="search">Tìm kiếm theo tên quy tắc hoặc tên cảm biến</param>
        /// <param name="isActive">Lọc theo trạng thái active/inactive</param>
        /// <param name="priority">Lọc theo mức độ ưu tiên (High, Medium, Low...)</param>
        /// <param name="pageNumber">Số trang (bắt đầu từ 1). Chỉ phân trang khi truyền cả pageNumber và pageSize</param>
        /// <param name="pageSize">Số lượng mỗi trang. Chỉ phân trang khi truyền cả pageNumber và pageSize</param>
        [HttpGet("rules")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> GetAllRulesAsync(
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? priority = null,
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null)
        {
            try
            {
                if (pageNumber.HasValue && pageNumber.Value < 1)
                    return BadRequest(new { message = "pageNumber phải lớn hơn hoặc bằng 1" });
                if (pageSize.HasValue && pageSize.Value < 1)
                    return BadRequest(new { message = "pageSize phải lớn hơn hoặc bằng 1" });

                var (rules, totalCount) = await _alertService.GetAllRulesAsync(search, isActive, priority, pageNumber, pageSize);

                int? totalPages = (pageNumber.HasValue && pageSize.HasValue)
                    ? (int)Math.Ceiling((double)totalCount / pageSize.Value)
                    : null;

                var ruleDtos = rules.Select(r => new AlertRuleDto
                {
                    RuleId = r.RuleId,
                    SensorId = r.SensorId,
                    SensorName = r.Sensor?.Name,
                    SiteId = r.Sensor?.Hub?.SiteId,
                    SiteName = r.Sensor?.Hub?.Site?.Name,
                    HubId = r.Sensor?.HubId,
                    HubName = r.Sensor?.Hub?.Name,
                    Name = r.Name,
                    ConditionType = r.ConditionType,
                    MinVal = r.MinVal,
                    MaxVal = r.MaxVal,
                    NotificationMethod = r.NotificationMethod,
                    Priority = r.Priority,
                    IsActive = r.IsActive
                }).ToList();

                return Ok(new
                {
                    message = "Lấy danh sách quy tắc cảnh báo thành công",
                    totalCount,
                    pageNumber,
                    pageSize,
                    totalPages,
                    data = ruleDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách quy tắc: " + ex.Message });
            }
        }

        /// <summary>
        /// Create Alert Rule
        /// </summary>
        [HttpPost("rules")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> CreateRuleAsync([FromBody] CreateAlertRuleDto request)
        {
            try
            {
                // Validate rule name
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên quy tắc không được để trống" });

                if (request.Name.Length < 2)
                    return BadRequest(new { message = "Tên quy tắc phải có ít nhất 2 ký tự" });

                // Validate SensorId
                if (request.SensorId <= 0)
                    return BadRequest(new { message = "SensorId không hợp lệ. Vui lòng chọn cảm biến cho quy tắc" });

                // Validate Min/Max values
                if (request.MinVal.HasValue && request.MaxVal.HasValue)
                {
                    if (request.MinVal.Value >= request.MaxVal.Value)
                        return BadRequest(new { message = "Giá trị tối thiểu phải nhỏ hơn giá trị tối đa" });
                }

                // Validate condition type
                if (string.IsNullOrWhiteSpace(request.ConditionType))
                    return BadRequest(new { message = "ConditionType không được để trống" });

                var rule = new AlertRule
                {
                    SensorId = request.SensorId,
                    Name = request.Name,
                    ConditionType = request.ConditionType,
                    MinVal = request.MinVal,
                    MaxVal = request.MaxVal,
                    NotificationMethod = request.NotificationMethod,
                    Priority = request.Priority,
                    IsActive = true
                };

                await _alertService.CreateRuleAsync(rule);

                return Ok(new
                {
                    message = "Tạo quy tắc cảnh báo thành công",
                    ruleId = rule.RuleId,
                    sensorId = rule.SensorId,
                    name = rule.Name,
                    conditionType = rule.ConditionType
                });
            }
            catch (Exception ex)
            {
                // Handle foreign key constraint
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK_"))
                {
                    if (ex.Message.Contains("SensorId"))
                        return BadRequest(new { message = "SensorId không tồn tại trong hệ thống. Vui lòng chọn cảm biến hợp lệ" });
                }

                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique"))
                    return BadRequest(new { message = "Quy tắc cảnh báo tương tự đã tồn tại cho cảm biến này" });

                return BadRequest(new { message = "Lỗi khi tạo quy tắc cảnh báo: " + ex.Message });
            }
        }



        /// <summary>
        /// Update Alert Rule
        /// </summary>
        [HttpPut("rules/{id}")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> UpdateRuleAsync(int id, [FromBody] UpdateAlertRuleDto request)
        {
            try
            {
                var rule = await _alertService.GetRuleByIdAsync(id);
                if (rule == null)
                    return NotFound(new { message = "Không tìm thấy quy tắc với ID: " + id });

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    if (request.Name.Length < 2)
                        return BadRequest(new { message = "Tên quy tắc phải có ít nhất 2 ký tự" });
                    rule.Name = request.Name;
                }

                if (!string.IsNullOrWhiteSpace(request.ConditionType))
                    rule.ConditionType = request.ConditionType;

                if (request.MinVal.HasValue)
                    rule.MinVal = request.MinVal.Value;

                if (request.MaxVal.HasValue)
                    rule.MaxVal = request.MaxVal.Value;

                if (rule.MinVal.HasValue && rule.MaxVal.HasValue && rule.MinVal.Value >= rule.MaxVal.Value)
                    return BadRequest(new { message = "Giá trị tối thiểu phải nhỏ hơn giá trị tối đa" });

                if (!string.IsNullOrWhiteSpace(request.NotificationMethod))
                    rule.NotificationMethod = request.NotificationMethod;

                if (!string.IsNullOrWhiteSpace(request.Priority))
                    rule.Priority = request.Priority;

                if (request.IsActive.HasValue)
                    rule.IsActive = request.IsActive.Value;

                await _alertService.UpdateRuleAsync(rule);

                return Ok(new
                {
                    message = "Cập nhật quy tắc thành công",
                    ruleId = rule.RuleId,
                    name = rule.Name
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật quy tắc: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete Alert Rule
        /// </summary>
        [HttpDelete("rules/{id}")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> DeleteRuleAsync(int id)
        {
            try
            {
                var rule = await _alertService.GetRuleByIdAsync(id);
                if (rule == null)
                    return NotFound(new { message = "Không tìm thấy quy tắc với ID: " + id });

                await _alertService.DeleteRuleAsync(id);

                return Ok(new
                {
                    message = "Xóa quy tắc cảnh báo thành công",
                    ruleId = id,
                    name = rule.Name
                });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("constraint") || ex.Message.Contains("REFERENCE"))
                    return BadRequest(new { message = "Không thể xóa quy tắc này vì còn lịch sử cảnh báo liên quan" });

                return BadRequest(new { message = "Lỗi khi xóa quy tắc: " + ex.Message });
            }
        }
    }
}
