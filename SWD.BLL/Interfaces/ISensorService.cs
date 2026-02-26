using SWD.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWD.BLL.Interfaces
{
    public interface ISensorService
    {
        Task<(List<Sensor> Sensors, int TotalCount)> GetAllSensorsAsync(int? hubId = null, int? typeId = null, string? search = null, string? status = null, int? siteId = null, int? pageNumber = null, int? pageSize = null);
        Task<Sensor?> GetSensorByIdAsync(int sensorId);       
        Task ProcessReadingAsync(int sensorId, float value);
        Task<List<SensorData>> GetSensorReadingsAsync(int sensorId, DateTime from, DateTime to);        
        Task<List<SensorType>> GetAllSensorTypesAsync();
        Task<List<Sensor>> GetSensorsByHubIdAsync(int hubId);    
        Task<List<Sensor>> GetSensorsByTypeIdAsync(int typeId);  
        Task RegisterSensorAsync(Sensor sensor);
        Task UpdateSensorStatusAsync(int sensorId, string status);
        Task UpdateSensorAsync(Sensor sensor);
        Task DeleteSensorAsync(int sensorId);
    }
}
