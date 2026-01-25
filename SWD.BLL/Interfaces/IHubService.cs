using SWD.DAL.Models;

namespace SWD.BLL.Interfaces
{
    /// <summary>
    /// Service for Hub/Gateway management operations
    /// </summary>
    public interface IHubService
    {
        Task<Hub?> GetHubByMacAsync(string macAddress);
        Task<Hub?> GetHubByIdAsync(int hubId);
        Task<List<Hub>> GetAllHubsAsync();
        Task<List<Hub>> GetHubsBySiteIdAsync(int siteId);
        Task CreateHubAsync(Hub hub);
        Task UpdateHubAsync(Hub hub);
        Task DeleteHubAsync(int hubId);
        Task<Hub?> GetHubWithReadingsAsync(int hubId, DateTime? from, DateTime? to);
        Task<List<Sensor>> GetHubCurrentTemperatureAsync(int hubId);
    }
}
