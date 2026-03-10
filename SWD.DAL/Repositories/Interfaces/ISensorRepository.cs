using SWD.DAL.Models;

namespace SWD.DAL.Repositories.Interfaces
{
    public interface ISensorRepository
    {
        Task<Sensor?> GetSensorByIdAsync(int sensorId);


        Task<List<Sensor>> GetAllSensorsAsync(int? hubId = null, int? typeId = null, string? search = null, string? status = null, int? siteId = null, string? sortBy = null, string? sortOrder = "asc");
        Task<List<SensorData>> GetReadingsForChartAsync(int hubId, DateTime from, DateTime to);
        Task<List<SensorType>> GetAllSensorTypesAsync();
        Task UpdateSensorAsync(Sensor sensor);
        Task AddSensorAsync(Sensor sensor);
        Task DeleteSensorAsync(int sensorId);
        Task AddReadingAsync(SensorData sensorData);
        Task SaveChangesAsync();
    }
}
