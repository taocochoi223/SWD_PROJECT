using SWD.DAL.Models;

namespace SWD.DAL.Repositories.Interfaces
{
    /// <summary>
    /// Repository for Hub/Gateway management
    /// </summary>
    public interface IHubRepository
    {
        Task<Hub?> GetHubByMacAddressAsync(string macAddress);
        Task<Hub?> GetHubByIdAsync(int hubId);
        Task<List<Hub>> GetAllHubsAsync();
        Task<List<Hub>> GetHubsBySiteIdAsync(int siteId);
        Task AddHubAsync(Hub hub);
        Task DeleteHubAsync(int hubId);
        Task UpdateHubAsync(Hub hub);
        Task<Hub?> GetHubWithReadingsAsync(int hubId, DateTime? from, DateTime? to);
        Task<List<Sensor>> GetHubTemperatureSensorsAsync(int hubId);
        Task SaveChangesAsync();

    }
}
