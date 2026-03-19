using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.BLL.Services
{
    public class SensorService : ISensorService
    {
        private readonly ISensorRepository _sensorRepo;
        private readonly IAlertService _alertService;

        public SensorService(ISensorRepository sensorRepo, IAlertService alertService)
        {
            _sensorRepo = sensorRepo;
            _alertService = alertService;
        }

        public async Task<(List<Sensor> Sensors, int TotalCount)> GetAllSensorsAsync(int? hubId = null, int? typeId = null, string? search = null, string? status = null, int? siteId = null, int? pageNumber = null, int? pageSize = null, string? sortBy = null, string? sortOrder = "asc")
        {
            var sensors = await _sensorRepo.GetAllSensorsAsync(hubId, typeId, search, status, siteId, sortBy, sortOrder);
            var totalCount = sensors.Count;

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                sensors = sensors.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
            }

            return (sensors, totalCount);
        }

        public async Task<Sensor?> GetSensorByIdAsync(int sensorId)
        {
            return await _sensorRepo.GetSensorByIdAsync(sensorId);
        }
        public async Task<List<Sensor>> GetSensorsByHubIdAsync(int hubId)
        {
            return await _sensorRepo.GetAllSensorsAsync(hubId: hubId);
        }
        public async Task<List<Sensor>> GetSensorsByTypeIdAsync(int typeId)
        {
            return await _sensorRepo.GetAllSensorsAsync(typeId: typeId);
        }
        public async Task RegisterSensorAsync(Sensor sensor)
        {
            await _sensorRepo.AddSensorAsync(sensor);
            await _sensorRepo.SaveChangesAsync();
        }
        public async Task<List<SensorData>> GetSensorReadingsAsync(int hubId, DateTime from, DateTime to)
        {
            return await _sensorRepo.GetReadingsForChartAsync(hubId, from, to);
        }

        public async Task<List<SensorType>> GetAllSensorTypesAsync()
        {
            return await _sensorRepo.GetAllSensorTypesAsync();
        }

        public async Task UpdateSensorAsync(Sensor sensor)
        {
            await _sensorRepo.UpdateSensorAsync(sensor);
            await _sensorRepo.SaveChangesAsync();
        }

        public async Task ProcessHubDataAsync(int hubId, string jsonValue)
        {
            var sensorData = new SensorData
            {
                HubId = hubId,
                JsonValue = jsonValue,
                RecordedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            };

            // CHẠY TUẦN TỰ ĐỂ TRÁNH LỖI CONCURRENCY CỦA ENTITY FRAMEWORK CORE
            await SaveReadingToDbAsync(sensorData);
            await _alertService.CheckAndTriggerAlertAsync(sensorData);
        }

        private async Task SaveReadingToDbAsync(SensorData sensorData)
        {
            await _sensorRepo.AddReadingAsync(sensorData);
            await _sensorRepo.SaveChangesAsync();
        }

        public async Task UpdateSensorStatusAsync(int sensorId, string status)
        {
            var sensor = await _sensorRepo.GetSensorByIdAsync(sensorId);
            if (sensor != null)
            {
                sensor.Status = status;
                await _sensorRepo.UpdateSensorAsync(sensor);
                await _sensorRepo.SaveChangesAsync();
            }
        }
        public async Task DeleteSensorAsync(int sensorId)
        {
            await _sensorRepo.DeleteSensorAsync(sensorId);
            await _sensorRepo.SaveChangesAsync();
        }
    }
}
