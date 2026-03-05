using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.BLL.Services
{
    public class HubService : IHubService
    {
        private readonly IHubRepository _hubRepo;

        public HubService(IHubRepository hubRepo)
        {
            _hubRepo = hubRepo;
        }

        public async Task<Hub?> GetHubByMacAsync(string macAddress)
        {
            return await _hubRepo.GetHubByMacAddressAsync(macAddress);
        }

        public async Task<Hub?> GetHubByIdAsync(int hubId)
        {
            return await _hubRepo.GetHubByIdAsync(hubId);
        }
        public async Task<(List<Hub> Hubs, int TotalCount)> GetAllHubsAsync(string? search = null, bool? isOnline = null, int? siteId = null, int? pageNumber = null, int? pageSize = null, string? sortBy = null, string? sortOrder = "asc")
        {
            var hubs = await _hubRepo.GetAllHubsAsync(search, isOnline, siteId, sortBy, sortOrder);
            var totalCount = hubs.Count;

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                hubs = hubs.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
            }

            return (hubs, totalCount);
        }
        public async Task CreateHubAsync(Hub hub)
        {
            await _hubRepo.AddHubAsync(hub);
            await _hubRepo.SaveChangesAsync();
        }

        public async Task UpdateHubAsync(Hub hub)
        {
            await _hubRepo.UpdateHubAsync(hub);
            await _hubRepo.SaveChangesAsync();
        }

        public async Task DeleteHubAsync(int hubId)
        {
            await _hubRepo.DeleteHubAsync(hubId);
            await _hubRepo.SaveChangesAsync();
        }

        public async Task<Hub?> GetHubWithReadingsAsync(int hubId, DateTime? from, DateTime? to)
        {
             return await _hubRepo.GetHubWithReadingsAsync(hubId, from, to);
        }

        public async Task<List<Sensor>> GetHubCurrentTemperatureAsync(int hubId)
        {
            return await _hubRepo.GetHubTemperatureSensorsAsync(hubId);
        }
    }
}
