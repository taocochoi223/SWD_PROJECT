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

        public async Task<List<Hub>> GetAllHubsAsync(string? search = null, bool? isOnline = null, int? siteId = null, string? sortBy = null, string? sortOrder = "asc")
        {
            var query = _context.Hubs
                .Include(s => s.Sensors)
                .Include(s => s.Site)
                .AsQueryable();

            if (siteId.HasValue)
            {
                query = query.Where(h => h.SiteId == siteId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(h =>
                    (h.Name != null && h.Name.ToLower().Contains(searchLower)) ||
                    h.MacAddress.ToLower().Contains(searchLower));
            }

            if (isOnline.HasValue)
            {
                query = query.Where(h => h.IsOnline == isOnline.Value);
            }

            bool isDesc = sortOrder?.ToLower() == "desc";
            query = sortBy?.ToLower() switch {
                "name"          => isDesc ? query.OrderByDescending(h => h.Name)          : query.OrderBy(h => h.Name),
                "macaddress"    => isDesc ? query.OrderByDescending(h => h.MacAddress)    : query.OrderBy(h => h.MacAddress),
                "isonline"      => isDesc ? query.OrderByDescending(h => h.IsOnline)      : query.OrderBy(h => h.IsOnline),
                "lasthandshake" => isDesc ? query.OrderByDescending(h => h.LastHandshake) : query.OrderBy(h => h.LastHandshake), // ← THÊM
                _               => isDesc ? query.OrderByDescending(h => h.HubId)         : query.OrderBy(h => h.HubId)
            };

            return await query.ToListAsync();
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
                .Include(h => h.SensorDatas)
                .AsQueryable();

            if (from.HasValue && to.HasValue)
            {
                 query = query.Where(h => h.HubId == hubId);
                 // Note: Filtering SensorDatas in query is more complex in EF core if not using projection
                 // For now, keeping it simple as this might be for dashboard
            }

            return await query.FirstOrDefaultAsync(h => h.HubId == hubId);
        }

        public async Task<List<Sensor>> GetHubTemperatureSensorsAsync(int hubId)
        {
            return await _context.Sensors
                .Include(s => s.Type)
                .Include(s => s.Hub)
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
