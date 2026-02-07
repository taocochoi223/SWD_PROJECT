using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;

namespace SWD.API.Controllers
{
    [Route("api/sensors")]
    [ApiController]
    [Authorize]
    public class SensorController : ControllerBase
    {
        private readonly ISensorService _sensorService;

        public SensorController(ISensorService sensorService)
        {
            _sensorService = sensorService;
        }

        /// <summary>
        /// Get All Sensors - With optional filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllSensorsAsync(
            [FromQuery] int? hub_id = null,
            [FromQuery] int? type = null)
        {
            try
            {
                var siteIdClaim = User.FindFirst("SiteId")?.Value;  
                int? userSiteId = !string.IsNullOrEmpty(siteIdClaim) ? int.Parse(siteIdClaim) : null;

                List<Sensor> sensors;

                if (hub_id.HasValue)
                {
                    sensors = await _sensorService.GetSensorsByHubIdAsync(hub_id.Value);
                }
                else if (type.HasValue)
                {
                    sensors = await _sensorService.GetSensorsByTypeIdAsync(type.Value);
                }
                else
                {
                    sensors = await _sensorService.GetAllSensorsAsync();
                }
                if(userSiteId.HasValue)
                {
                    sensors = sensors.Where(s => s.Hub != null && s.Hub.SiteId == userSiteId.Value).ToList();
                }

                var sensorDtos = sensors.Select(s => new SensorDto
                {
                    SensorId = s.SensorId,
                    HubId = s.HubId,
                    HubName = s.Hub?.Name,
                    TypeId = s.TypeId,
                    TypeName = s.Type?.TypeName,
                    SensorName = s.Name,
                    CurrentValue = 0,
                    LastUpdate = null,
                    Status = s.Status
                }).ToList();

                return Ok(new
                {
                    message = "Lấy danh sách cảm biến thành công",
                    count = sensorDtos.Count,
                    userSiteId = userSiteId, 
                    data = sensorDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách cảm biến: " + ex.Message });
            }
        }

        /// <summary>
        /// Register Sensor
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> RegisterSensorAsync([FromBody] RegisterSensorDto request)
        {
            try
            {
                // Validate sensor name
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Tên cảm biến không được để trống" });

                if (request.Name.Length < 2)
                    return BadRequest(new { message = "Tên cảm biến phải có ít nhất 2 ký tự" });

                // Validate HubId
                if (request.HubId <= 0)
                    return BadRequest(new { message = "HubId không hợp lệ. Vui lòng chọn Hub cho cảm biến" });

                // Validate TypeId
                if (request.TypeId <= 0)
                    return BadRequest(new { message = "TypeId không hợp lệ. Vui lòng chọn loại cảm biến" });

                var sensor = new Sensor
                {
                    HubId = request.HubId,
                    TypeId = request.TypeId,
                    Name = request.Name,
                    Status = "Active"
                };

                await _sensorService.RegisterSensorAsync(sensor);

                return Ok(new
                {
                    message = "Đăng ký cảm biến thành công",
                    sensor = new SensorDto
                    {
                        SensorId = sensor.SensorId,
                        HubId = sensor.HubId,
                        HubName = null,
                        TypeId = sensor.TypeId,
                        TypeName = null,
                        SensorName = sensor.Name,
                        CurrentValue = 0,
                        LastUpdate = null,
                        Status = sensor.Status
                    }
                });
            }
            catch (Exception ex)
            {
                // Handle specific errors
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK_"))
                {
                    if (ex.Message.Contains("HubId"))
                        return BadRequest(new { message = "HubId không tồn tại trong hệ thống. Vui lòng chọn Hub hợp lệ" });
                    if (ex.Message.Contains("TypeId"))
                        return BadRequest(new { message = "TypeId không tồn tại trong hệ thống. Vui lòng chọn loại cảm biến hợp lệ" });
                }

                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique"))
                    return BadRequest(new { message = "Tên cảm biến đã tồn tại trong Hub này. Vui lòng sử dụng tên khác" });

                return BadRequest(new { message = "Lỗi khi đăng ký cảm biến: " + ex.Message });
            }
        }

        /// <summary>
        /// Get Sensor Readings - For chart display
        /// </summary>
        [HttpGet("{id}/readings")]
        public async Task<IActionResult> GetReadingsAsync(
            int id,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                // Validate sensor ID
                if (id <= 0)
                    return BadRequest(new { message = "SensorId không hợp lệ" });

                // Check if sensor exists
                var sensor = await _sensorService.GetSensorByIdAsync(id);
                if (sensor == null)
                    return NotFound(new { message = "Không tìm thấy cảm biến với ID: " + id });

                DateTime fromDate;
                DateTime toDate;

                if (from.HasValue && to.HasValue)
                {
                    // Validate date range
                    if (from.Value > to.Value)
                        return BadRequest(new { message = "Ngày bắt đầu không được lớn hơn ngày kết thúc" });

                    fromDate = from.Value.Date;
                    toDate = to.Value.Date.AddDays(1).AddTicks(-1);
                }
                else
                {
                    fromDate = DateTime.MinValue;
                    toDate = DateTime.MaxValue;
                }

                var readings = await _sensorService.GetSensorReadingsAsync(id, fromDate, toDate);

                var readingDtos = readings.Select(r => new SensorDataDto
                {
                    DataId = r.DataId,
                    SensorId = r.SensorId,
                    HubId = r.HubId,
                    SensorName = r.Sensor?.Name,
                    SensorTypeName = r.Sensor?.Type?.TypeName,
                    Value = r.Value,
                    RecordedAt = r.RecordedAt
                }).ToList();

                return Ok(new
                {
                    message = readingDtos.Count > 0 
                        ? "Lấy dữ liệu đo của cảm biến thành công" 
                        : "Không có dữ liệu đo trong khoảng thời gian này",
                    sensorId = id,
                    sensorName = sensor.Name,
                    count = readingDtos.Count,
                    fromDate = from,
                    toDate = to,
                    data = readingDtos
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu đo: " + ex.Message });
            }
        }

        /// <summary>
        /// Get All Sensor Types - For dropdown
        /// </summary>
        [HttpGet("types")]
        public async Task<IActionResult> GetAllTypesAsync()
        {
            try
            {
                var types = await _sensorService.GetAllSensorTypesAsync();
                return Ok(new
                {
                    message = "Lấy danh sách loại cảm biến thành công",
                    count = types.Count,
                    data = types
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách loại cảm biến: " + ex.Message });
            }
        }

        /// <summary>
        /// Update Sensor
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> UpdateSensorAsync(int id, [FromBody] UpdateSensorDto request)
        {
            try
            {
                if (id <= 0) return BadRequest(new { message = "SensorId không hợp lệ" });

                var sensor = await _sensorService.GetSensorByIdAsync(id);
                if (sensor == null) return NotFound(new { message = "Không tìm thấy cảm biến" });

                if (!string.IsNullOrEmpty(request.Name))
                {
                    if (request.Name.Length < 2) return BadRequest(new { message = "Tên quá ngắn" });
                    sensor.Name = request.Name;
                }

                if (request.TypeId.HasValue && request.TypeId > 0)
                {
                    sensor.TypeId = request.TypeId.Value;
                }

                await _sensorService.UpdateSensorAsync(sensor);
                return Ok(new { message = "Cập nhật cảm biến thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi cập nhật: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete Sensor
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ADMIN,Manager,MANAGER")]
        public async Task<IActionResult> DeleteSensorAsync(int id)
        {
            try
            {
                if (id <= 0) return BadRequest(new { message = "SensorId không hợp lệ" });
                
                var sensor = await _sensorService.GetSensorByIdAsync(id);
                if (sensor == null) return NotFound(new { message = "Không tìm thấy cảm biến" });

                await _sensorService.DeleteSensorAsync(id);
                return Ok(new { message = "Xóa cảm biến thành công" });
            }
            catch (Exception ex) 
            {
                return BadRequest(new { message = "Lỗi xóa cảm biến (Có thể do còn dữ liệu ràng buộc): " + ex.Message });
            }
        }
    }
}