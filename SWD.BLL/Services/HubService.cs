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
        public async Task<List<Hub>> GetAllHubsAsync()
        {
            return await _hubRepo.GetAllHubsAsync();
        }
        public async Task<List<Hub>> GetHubsBySiteIdAsync(int siteId)
        {
            return await _hubRepo.GetHubsBySiteIdAsync(siteId);
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
