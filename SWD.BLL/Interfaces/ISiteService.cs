using SWD.DAL.Models;

namespace SWD.BLL.Interfaces
{
    /// <summary>
    /// Service for Site management operations
    /// </summary>
    public interface ISiteService
    {
        Task<List<Site>> GetAllSitesAsync();
        Task<(List<Site> Sites, int TotalCount)> GetAllSitesAsync(string? search, int? orgId, int? pageNumber, int? pageSize);
        Task<Site?> GetSiteByIdAsync(int siteId);
        Task CreateSiteAsync(Site site);
        Task UpdateSiteAsync(Site site);
        Task DeleteSiteAsync(int siteId);
        Task<List<Site>> GetSiteHierarchyAsync();
        Task<Site?> GetSiteHierarchyByIdAsync(int siteId);
    }
}
