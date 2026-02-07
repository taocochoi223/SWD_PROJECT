using Microsoft.EntityFrameworkCore;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.DAL.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for Hub/Gateway management
    /// </summary>
    public class HubRepository : IHubRepository
    {
        private readonly IoTFinalDbContext _context;

        public HubRepository(IoTFinalDbContext context)
        {
            _context = context;
        }

        public async Task<Hub?> GetHubByMacAddressAsync(string macAddress)
        {
            return await _context.Hubs
                .Include(h => h.Site)
                .FirstOrDefaultAsync(h => h.MacAddress == macAddress);
        }

        public async Task<Hub?> GetHubByIdAsync(int hubId)
        {
            return await _context.Hubs
                .Include(h => h.Site)
                .Include(h => h.Sensors)
                .FirstOrDefaultAsync(h => h.HubId == hubId);
        }
        public async Task<List<Hub>> GetAllHubsAsync()
        {
            var hubs = await _context.Hubs
                .Include(s => s.Sensors)
                .Include(s => s.Site)
                .ToListAsync();
            return hubs;
        }
        public async Task<List<Hub>> GetHubsBySiteIdAsync(int siteId)
        {
            var hubs = await _context.Hubs
                .Where(h => h.SiteId == siteId)
                .Include(s => s.Sensors)
                .Include(s => s.Site)
                .ToListAsync();
            return hubs;
        }
        public async Task AddHubAsync(Hub hub)
        {
            await _context.Hubs.AddAsync(hub);
        }

        public async Task DeleteHubAsync(int hubId)
        {
            var hub = await _context.Hubs.FindAsync(hubId);
            if (hub != null)
            {
                _context.Hubs.Remove(hub);
            }
        }

        public Task UpdateHubAsync(Hub hub)
        {
            _context.Hubs.Update(hub);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Hub?> GetHubWithReadingsAsync(int hubId, DateTime? from, DateTime? to)
        {
            var query = _context.Hubs
                .Include(h => h.Sensors)
                .ThenInclude(s => s.Type)
                .AsQueryable();

            if (from.HasValue && to.HasValue)
            {
                 query = query.Include(h => h.Sensors)
                              .ThenInclude(s => s.SensorDatas.Where(r => r.RecordedAt >= from && r.RecordedAt <= to));
            }
            else
            {
                 // Limit to last 100 if no date range to prevent explosion, or just include all
                 // For now, let's include all but maybe user should provide range
                 query = query.Include(h => h.Sensors)
                              .ThenInclude(s => s.SensorDatas);
            }

            return await query.FirstOrDefaultAsync(h => h.HubId == hubId);
        }

        public async Task<List<Sensor>> GetHubTemperatureSensorsAsync(int hubId)
        {
            return await _context.Sensors
                .Include(s => s.Type)
                .Include(s => s.Hub)
                .Include(s => s.SensorDatas)
                .Where(s => s.HubId == hubId && 
                       (s.Type.TypeName.Contains("Temperature") || 
                        s.Type.TypeName.Contains("Nhiệt độ") ||
                        s.Type.TypeName.Contains("Humidity") ||
                        s.Type.TypeName.Contains("Độ ẩm") ||
                        s.Type.TypeName.Contains("Pressure") ||
                        s.Type.TypeName.Contains("Áp suất")))
                .ToListAsync();
        }



    }
}
