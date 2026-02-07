using Microsoft.EntityFrameworkCore;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.DAL.Repositories.Implementations
{
    /// <summary>
    /// Repository quản lý Sensor, Reading, AlertRule và AlertHistory
    /// (Core 80% workload của hệ thống IoT)
    /// </summary>
    public class SensorRepository : ISensorRepository
    {
        private readonly IoTFinalDbContext _context;

        public SensorRepository(IoTFinalDbContext context)
        {
            _context = context;
        }

        public async Task<Sensor?> GetSensorByIdAsync(int sensorId)
        {
            return await _context.Sensors
                .Include(s => s.Hub)
                .FirstOrDefaultAsync(s => s.SensorId == sensorId);
        }
        public async Task<List<Sensor>> GetSensorsByHubIdAsync(int hubId)
        {
            return await _context.Sensors
                .Include(h => h.Hub)
                .Include(t => t.Type)
                .Where(s => s.HubId == hubId)
                .ToListAsync();
        }

        public async Task<List<Sensor>> GetSensorsByTypeIdAsync(int typeId)
        {
            return await _context.Sensors
                .Include(s => s.Hub)
                .Include(s => s.Type)
                .Where(s => s.TypeId == typeId)
                .ToListAsync();
        }
        public Task UpdateSensorAsync(Sensor sensor)
        {
            _context.Sensors.Update(sensor);
            return Task.CompletedTask;
        }

        public async Task AddSensorAsync(Sensor sensor)
        {
            await _context.Sensors.AddAsync(sensor);
        }

        public async Task<List<Sensor>> GetAllSensorsWithDetailsAsync()
        {

            
            return await _context.Sensors
                .Include(s => s.Hub)
                .Include(s => s.Type)
                .OrderBy(s => s.HubId)
                .ToListAsync();
        }

        public async Task AddReadingAsync(SensorData sensorData)
        {
            await _context.SensorDatas.AddAsync(sensorData);
        }

        public async Task<List<SensorData>> GetReadingsForChartAsync(
            int sensorId,
            DateTime from,
            DateTime to)
        {
            return await _context.SensorDatas
                .AsNoTracking()
                .Include(r => r.Sensor)
                    .ThenInclude(s => s.Type)
                .Where(r => r.SensorId == sensorId &&
                            r.RecordedAt.HasValue &&
                            r.RecordedAt.Value >= from &&
                            r.RecordedAt.Value <= to)
                .OrderBy(r => r.RecordedAt)
                .ToListAsync();
        }
        
        public async Task<List<SensorType>> GetAllSensorTypesAsync()
        {
            return await _context.SensorTypes.ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSensorAsync(int sensorId)
        {
            var sensor = await _context.Sensors.FindAsync(sensorId);
            if (sensor != null)
            {
                _context.Sensors.Remove(sensor);
            }
        }
    }
}
