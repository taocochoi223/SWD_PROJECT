using SWD.DAL.Models;

namespace SWD.DAL.Repositories.Interfaces
{
    public interface ISensorRepository
    {
        Task<Sensor?> GetSensorByIdAsync(int sensorId);
        Task<List<Sensor>> GetSensorsByHubIdAsync(int hubId);
        Task<List<Sensor>> GetAllSensorsWithDetailsAsync();
        Task<List<SensorData>> GetReadingsForChartAsync(int sensorId,DateTime from, DateTime to);
        Task<List<SensorType>> GetAllSensorTypesAsync();
        Task<List<Sensor>> GetSensorsByTypeIdAsync(int typeId);
        Task UpdateSensorAsync(Sensor sensor);
        Task AddSensorAsync(Sensor sensor);
        Task DeleteSensorAsync(int sensorId);
        Task AddReadingAsync(SensorData sensorData);
        Task SaveChangesAsync();
    }
}
