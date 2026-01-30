using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD.API.Dtos;
using SWD.BLL.Interfaces;
using SWD.DAL.Models;

namespace SWD.API.Controllers
{
    [Route("api/sensors")]  // ← ĐỔI từ "api/sensor"
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

                var sensorDtos = sensors.Select(s => new SensorDto
                {
                    SensorId = s.SensorId,
                    HubId = s.HubId,
                    HubName = s.Hub?.Name,
                    TypeId = s.TypeId,
                    TypeName = s.Type?.TypeName,
                    SensorName = s.Name,
                    CurrentValue = s.CurrentValue,
                    LastUpdate = s.LastUpdate,
                    Status = s.Status
                }).ToList();

                return Ok(new
                {
                    message = "Lấy danh sách cảm biến thành công",
                    count = sensorDtos.Count,
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
                    Status = "Active",
                    CurrentValue = 0,
                    LastUpdate = DateTime.UtcNow
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
                        CurrentValue = sensor.CurrentValue,
                        LastUpdate = sensor.LastUpdate,
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

                var readingDtos = readings.Select(r => new ReadingDto
                {
                    ReadingId = r.ReadingId,
                    SensorId = r.SensorId,
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
        /// Receive Telemetry - IoT Gateway sends data here
        /// </summary>
        [HttpPost("telemetry")]
        [AllowAnonymous]
        public async Task<IActionResult> ReceiveTelemetry(
            [FromQuery] int sensorId,
            [FromQuery] float value)
        {
            try
            {
                // Validate sensor ID
                if (sensorId <= 0)
                    return BadRequest(new { message = "SensorId không hợp lệ" });

                // Validate value range (reasonable limits)
                if (float.IsNaN(value) || float.IsInfinity(value))
                    return BadRequest(new { message = "Giá trị đo không hợp lệ (NaN hoặc Infinity)" });

                if (value < -1000 || value > 10000)
                    return BadRequest(new { message = "Giá trị đo nằm ngoài phạm vi cho phép (-1000 đến 10000)" });

                await _sensorService.ProcessReadingAsync(sensorId, value);
                return Ok(new
                {
                    message = "Nhận dữ liệu telemetry thành công",
                    sensorId = sensorId,
                    value = value,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Check if sensor doesn't exist
                if (ex.Message.Contains("not found") || ex.Message.Contains("does not exist"))
                    return NotFound(new { message = "Không tìm thấy cảm biến với ID: " + sensorId });

                return BadRequest(new
                {
                    message = "Lỗi khi xử lý dữ liệu telemetry",
                    sensorId = sensorId,
                    error = ex.Message
                });
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
    }
}