using Microsoft.EntityFrameworkCore;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.DAL.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for Site management
    /// </summary>
    public class SiteRepository : ISiteRepository
    {
        private readonly IoTFinalDbContext _context;

        public SiteRepository(IoTFinalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Site>> GetAllSitesAsync()
        {
            return await _context.Sites
                .Include(s => s.Org)
                .Include(s => s.Hubs)
                .ToListAsync();
        }

        public async Task<Site?> GetSiteByIdAsync(int siteId)
        {
            return await _context.Sites
                .Include(s => s.Org)
                .Include(s => s.Hubs)
                    .ThenInclude(h => h.Sensors)
                .FirstOrDefaultAsync(s => s.SiteId == siteId);
        }

        public async Task AddSiteAsync(Site site)
        {
            await _context.Sites.AddAsync(site);
        }

        public Task UpdateSiteAsync(Site site)
        {
            _context.Sites.Update(site);
            return Task.CompletedTask;
        }

        public async Task DeleteSiteAsync(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site != null)
            {
                _context.Sites.Remove(site);
            }
        }

        public async Task<List<Site>> GetSiteHierarchyAsync()
        {
            return await _context.Sites
                .Include(s => s.Hubs)
                    .ThenInclude(h => h.Sensors)
                        .ThenInclude(se => se.SensorDatas)
                 .Include(s => s.Hubs)
                    .ThenInclude(h => h.Sensors)
                        .ThenInclude(se => se.Type)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Site?> GetSiteHierarchyByIdAsync(int siteId)
        {
            return await _context.Sites
                .Include(s => s.Hubs)
                    .ThenInclude(h => h.Sensors)
                        .ThenInclude(se => se.SensorDatas)
                 .Include(s => s.Hubs)
                    .ThenInclude(h => h.Sensors)
                        .ThenInclude(se => se.Type)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SiteId == siteId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
