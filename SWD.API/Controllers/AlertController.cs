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
        /// Get Alerts - With optional filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAlertsAsync(
            [FromQuery] string? status = "All",
            [FromQuery] string? search = null)
        {
            try
            {
                var alerts = await _alertService.GetAlertsWithFiltersAsync(status, search);

                var alertDtos = alerts.Select(h => new
                {
                    id = h.HistoryId,
                    time = h.TriggeredAt,
                    sensor_name = h.Sensor?.Name ?? "Unknown",
                    severity = h.Severity ?? "Warning",
                    status = h.ResolvedAt == null ? "Active" : "Resolved"
                }).ToList();

                return Ok(new 
                {
                    message = "Lấy danh sách cảnh báo thành công",
                    count = alertDtos.Count,
                    data = alertDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách cảnh báo: " + ex.Message });
            }
        }

        /// <summary>
        /// Resolve Alert
        /// </summary>
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> ResolveAlertAsync(int id)
        {
            try
            {
                var alert = await _alertService.GetAlertByIdAsync(id);
                if (alert == null)
                    return NotFound(new { message = "Không tìm thấy cảnh báo với ID: " + id });

                if (alert.ResolvedAt != null)
                    return BadRequest(new { message = "Cảnh báo này đã được xử lý trước đó" });

                await _alertService.ResolveAlertAsync(id);

                return Ok(new
                {
                    message = "Xử lý cảnh báo thành công",
                    id = id,
                    resolvedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xử lý cảnh báo: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete Alert Log
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ADMIN")]
        public async Task<IActionResult> DeleteAlertAsync(int id)
        {
            try
            {
                var alert = await _alertService.GetAlertByIdAsync(id);
                if (alert == null)
                    return NotFound(new { message = "Không tìm thấy cảnh báo với ID: " + id });

                await _alertService.DeleteAlertAsync(id);

                return Ok(new { message = "Xóa cảnh báo thành công", id = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xóa cảnh báo: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Alert Rules - For configuration
        /// </summary>
        [HttpGet("rules")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> GetAllRulesAsync()
        {
            try
            {
                var rules = await _alertService.GetAllRulesAsync();

                var ruleDtos = rules.Select(r => new AlertRuleDto
                {
                    RuleId = r.RuleId,
                    SensorId = r.SensorId,
                    SensorName = r.Sensor?.Name,
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
                    count = ruleDtos.Count,
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
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên quy tắc không được để trống" });

                if (request.SensorId <= 0)
                    return BadRequest(new { message = "SensorId không hợp lệ" });

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
                    name = rule.Name
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi tạo quy tắc cảnh báo: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Alert History - For historical analysis
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryAsync([FromQuery] int? sensorId = null)
        {
            try
            {
                var history = await _alertService.GetAlertHistoryAsync(sensorId);

                var historyDtos = history.Select(h => new AlertHistoryDto
                {
                    HistoryId = h.HistoryId,
                    RuleId = h.RuleId,
                    RuleName = h.Rule?.Name,
                    SensorId = h.SensorId,
                    SensorName = h.Sensor?.Name,
                    TriggeredAt = h.TriggeredAt,
                    ResolvedAt = h.ResolvedAt,
                    ValueAtTrigger = h.ValueAtTrigger,
                    Severity = h.Severity,
                    Message = h.Message
                }).ToList();

                return Ok(new
                {
                    message = "Lấy lịch sử cảnh báo thành công",
                    count = historyDtos.Count,
                    sensorId = sensorId,
                    data = historyDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy lịch sử cảnh báo: " + ex.Message });
            }
        }
    }
}
