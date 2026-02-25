using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.BLL.Services
{
    public class SiteService : ISiteService
    {
        private readonly ISiteRepository _siteRepo;

        public SiteService(ISiteRepository siteRepo)
        {
            _siteRepo = siteRepo;
        }

        public async Task<List<Site>> GetAllSitesAsync()
        {
            return await _siteRepo.GetAllSitesAsync();
        }

        public async Task<(List<Site> Sites, int TotalCount)> GetAllSitesAsync(string? search, int? orgId, int? pageNumber, int? pageSize)
        {
            var sites = await _siteRepo.GetAllSitesAsync(search, orgId);
            var totalCount = sites.Count;

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                sites = sites.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
            }

            return (sites, totalCount);
        }

        public async Task<Site?> GetSiteByIdAsync(int siteId)
        {
            return await _siteRepo.GetSiteByIdAsync(siteId);
        }

        public async Task CreateSiteAsync(Site site)
        {
            await _siteRepo.AddSiteAsync(site);
            await _siteRepo.SaveChangesAsync();
        }

        public async Task UpdateSiteAsync(Site site)
        {
            await _siteRepo.UpdateSiteAsync(site);
            await _siteRepo.SaveChangesAsync();
        }

        public async Task DeleteSiteAsync(int siteId)
        {
            await _siteRepo.DeleteSiteAsync(siteId);
            await _siteRepo.SaveChangesAsync();
        }

        public async Task<List<Site>> GetSiteHierarchyAsync()
        {
            return await _siteRepo.GetSiteHierarchyAsync();
        }

        public async Task<Site?> GetSiteHierarchyByIdAsync(int siteId)
        {
            return await _siteRepo.GetSiteHierarchyByIdAsync(siteId);
        }
    }
}
