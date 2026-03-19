using Microsoft.EntityFrameworkCore;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.DAL.Repositories.Implementations
{

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
                    .ThenInclude(h => h.Site)
                .Include(s => s.Hub)
                    .ThenInclude(h => h.AlertRules)
                .Include(s => s.Type)
                .FirstOrDefaultAsync(s => s.SensorId == sensorId);
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



        public async Task<List<Sensor>> GetAllSensorsAsync(int? hubId = null, int? typeId = null, string? search = null, string? status = null, int? siteId = null, string? sortBy = null, string? sortOrder = "asc")
        {
            var query = _context.Sensors
                .Include(s => s.Hub)
                    .ThenInclude(h => h.AlertRules)
                .Include(s => s.Type)
                .AsQueryable();

            if (siteId.HasValue)
            {
                query = query.Where(s => s.Hub != null && s.Hub.SiteId == siteId.Value);
            }

            if (hubId.HasValue)
            {
                query = query.Where(s => s.HubId == hubId.Value);
            }

            if (typeId.HasValue)
            {
                query = query.Where(s => s.TypeId == typeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(s => s.Name != null && s.Name.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusTrimmed = status.Trim().ToLower();
                query = query.Where(s => s.Status != null && s.Status.ToLower() == statusTrimmed);
            }

            bool isDesc = sortOrder?.ToLower() == "desc";
            query = sortBy?.ToLower() switch {
                "name"     => isDesc ? query.OrderByDescending(s => s.Name)     : query.OrderBy(s => s.Name),
                "status"   => isDesc ? query.OrderByDescending(s => s.Status)   : query.OrderBy(s => s.Status),
                "hubid"    => isDesc ? query.OrderByDescending(s => s.HubId)    : query.OrderBy(s => s.HubId),
                "type"     => isDesc ? query.OrderByDescending(s => s.TypeId)   : query.OrderBy(s => s.TypeId),
                _          => isDesc ? query.OrderByDescending(s => s.SensorId) : query.OrderBy(s => s.SensorId)
            };
            return await query.ToListAsync();
        }

        public async Task AddReadingAsync(SensorData sensorData)
        {
            await _context.SensorDatas.AddAsync(sensorData);
        }

        public async Task<List<SensorData>> GetReadingsForChartAsync(
            int hubId,
            DateTime from,
            DateTime to)
        {
            return await _context.SensorDatas
                .AsNoTracking()
                .Include(r => r.Hub)
                .Where(r => r.HubId == hubId &&
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
