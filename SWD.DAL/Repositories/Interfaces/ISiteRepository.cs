using SWD.DAL.Models;

namespace SWD.DAL.Repositories.Interfaces
{
    /// <summary>
    /// Repository for Site (Store/Location) management
    /// </summary>
    public interface ISiteRepository
    {
        Task<List<Site>> GetAllSitesAsync(string? search = null, int? orgId = null);
        Task<Site?> GetSiteByIdAsync(int siteId);
        Task AddSiteAsync(Site site);
        Task UpdateSiteAsync(Site site);
        Task DeleteSiteAsync(int siteId);
        Task<List<Site>> GetSiteHierarchyAsync();
        Task<Site?> GetSiteHierarchyByIdAsync(int siteId);
        Task SaveChangesAsync();
    }
}
