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

        public async Task<List<Sensor>> GetAllSensorsAsync()
        {
            return await _sensorRepo.GetAllSensorsWithDetailsAsync();
        }

        public async Task<Sensor?> GetSensorByIdAsync(int sensorId)
        {
            return await _sensorRepo.GetSensorByIdAsync(sensorId);
        }
        public async Task<List<Sensor>> GetSensorsByHubIdAsync(int hubId)
        {
            return await _sensorRepo.GetSensorsByHubIdAsync(hubId);
        }
        public async Task<List<Sensor>> GetSensorsByTypeIdAsync(int typeId)
        {
            return await _sensorRepo.GetSensorsByTypeIdAsync(typeId);
        }
        public async Task RegisterSensorAsync(Sensor sensor)
        {
            await _sensorRepo.AddSensorAsync(sensor);
            await _sensorRepo.SaveChangesAsync();
        }
        public async Task<List<Reading>> GetSensorReadingsAsync(int sensorId, DateTime from, DateTime to)
        {
            return await _sensorRepo.GetReadingsForChartAsync(sensorId, from, to);
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

        public async Task ProcessReadingAsync(int sensorId, float value)
        {
            var reading = new Reading
            {
                SensorId = sensorId,
                Value = value,
                RecordedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            };

            await _sensorRepo.AddReadingAsync(reading);

            var sensor = await _sensorRepo.GetSensorByIdAsync(sensorId);
            if (sensor != null)
            {
                sensor.CurrentValue = value;
                sensor.LastUpdate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                await _sensorRepo.UpdateSensorAsync(sensor);
            }

            await _sensorRepo.SaveChangesAsync();

            await _alertService.CheckAndTriggerAlertAsync(reading);
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
    }
}
